using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using GUI.Models;

namespace GUI.Services
{
    public interface IRobotKinematicsService
    {
        // Trả về cả position và orientation
        (Vector3D Position, Vector3D Orientation) ForwardKinematics(IList<Joint> joints, double[] angles);

        double[] InverseKinematics(IList<Joint> joints, Vector3D targetPosition, Vector3D targetOrientation,
            double[] currentAngles, int maxIterations = 5000);

        double DistanceFromTarget(IList<Joint> joints, Vector3D targetPosition, Vector3D targetOrientation,
            double[] angles);
    }

    public class RobotKinematicsService : IRobotKinematicsService
    {
        private const double PositionThreshold = 0.001; // 1mm trong mét
        private const double OrientationThreshold = 1.0; // 1 độ
        private const double Damping = 0.1; // Hệ số damping cho LM
        private const int MaxIterations = 1000; // Ít hơn gradient descent vì LM hội tụ nhanh

        public (Vector3D Position, Vector3D Orientation) ForwardKinematics(IList<Joint> joints, double[] angles)
        {
            if (joints == null || joints.Count < 6 || angles.Length < 6)
                return (new Vector3D(0, 0, 0), new Vector3D(0, 0, 0));

            // Tính toán transformation matrix cho từng joint
            Matrix3D finalTransform = Matrix3D.Identity;

            for (int i = 0; i < 6; i++)
            {
                // Translation đến rotation point
                var translateToRotPoint = new Matrix3D();
                translateToRotPoint.Translate(new Vector3D(
                    joints[i].RotationPoint.X,
                    joints[i].RotationPoint.Y,
                    joints[i].RotationPoint.Z
                ));

                // Rotation quanh axis
                var rotation = new Matrix3D();
                rotation.Rotate(new Quaternion(joints[i].RotationAxis, angles[i]));

                // Translation ngược lại
                var translateBack = new Matrix3D();
                translateBack.Translate(new Vector3D(
                    -joints[i].RotationPoint.X,
                    -joints[i].RotationPoint.Y,
                    -joints[i].RotationPoint.Z
                ));

                // Combine transformations
                var jointTransform = translateBack * rotation * translateToRotPoint;
                finalTransform = jointTransform * finalTransform;

                // Apply transform to 3D model
                if (joints[i].Model != null)
                {
                    var transformGroup = new Transform3DGroup();
                    transformGroup.Children.Add(new MatrixTransform3D(finalTransform));
                    joints[i].Model.Transform = transformGroup;
                }
            }

            // Extract position từ final transform
            Vector3D position = new Vector3D(
                finalTransform.OffsetX,
                finalTransform.OffsetY,
                finalTransform.OffsetZ
            );

            // Extract orientation (Euler angles) từ rotation matrix
            Vector3D orientation = ExtractEulerAngles(finalTransform);

            // Nếu có model cho joint cuối, lấy position từ bounds center
            if (joints[5].Model != null && joints[5].Model.Bounds != Rect3D.Empty)
            {
                var bounds = joints[5].Model.Bounds;
                position = new Vector3D(
                    bounds.X + bounds.SizeX / 2,
                    bounds.Y + bounds.SizeY / 2,
                    bounds.Z + bounds.SizeZ / 2
                );
            }

            return (position, orientation);
        }

        public double[] InverseKinematics(IList<Joint> joints, Vector3D targetPosition,
            Vector3D targetOrientation, double[] currentAngles, int maxIterations = 1000)
        {
            var q = Vector<double>.Build.DenseOfArray(currentAngles); // Góc hiện tại (độ)
            var target = Vector<double>.Build.DenseOfArray(new[]
            {
                targetPosition.X, targetPosition.Y, targetPosition.Z,
                targetOrientation.X * Math.PI / 180.0, // Chuyển độ sang radian
                targetOrientation.Y * Math.PI / 180.0,
                targetOrientation.Z * Math.PI / 180.0
            });

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                // Tính vị trí và hướng hiện tại
                var (currentPos, currentOri) = ForwardKinematics(joints, q.ToArray());
                var current = Vector<double>.Build.DenseOfArray(new[]
                {
                    currentPos.X, currentPos.Y, currentPos.Z,
                    currentOri.X * Math.PI / 180.0, // Chuyển độ sang radian
                    currentOri.Y * Math.PI / 180.0,
                    currentOri.Z * Math.PI / 180.0
                });

                // Vector lỗi (6 DOF)
                var error = target - current;
                double errorNorm = error.L2Norm();
                Console.WriteLine($"Iteration {iteration}: PosError={error.SubVector(0, 3).L2Norm():F6}m, OriError={error.SubVector(3, 3).L2Norm() * 180.0 / Math.PI:F2}deg");

                // Kiểm tra hội tụ
                double posError = error.SubVector(0, 3).L2Norm();
                double oriError = error.SubVector(3, 3).L2Norm() * 180.0 / Math.PI; // Chuyển radian sang độ
                if (posError < PositionThreshold && oriError < OrientationThreshold)
                    break;

                // Tính Jacobian
                var J = ComputeJacobian(joints, q.ToArray());

                // Levenberg-Marquardt: (J^T * J + λ * I) * Δq = J^T * error
                var Jt = J.Transpose();
                var lambda = Damping * errorNorm;
                var A = Jt * J + lambda * Matrix<double>.Build.DenseIdentity(6);
                var b = Jt * error;
                var deltaQ = A.Solve(b);

                // Cập nhật góc
                for (int i = 0; i < 6; i++)
                {
                    q[i] += deltaQ[i];
                    q[i] = Math.Clamp(q[i], joints[i].AngleMin, joints[i].AngleMax);
                }
            }

            return q.ToArray();
        }

        private Matrix<double> ComputeJacobian(IList<Joint> joints, double[] angles)
        {
            var J = Matrix<double>.Build.Dense(6, 6);
            const double delta = 0.001; // Small perturbation

            for (int i = 0; i < 6; i++)
            {
                var anglesPlus = (double[])angles.Clone();
                anglesPlus[i] += delta;
                var (posPlus, oriPlus) = ForwardKinematics(joints, anglesPlus);
                var anglesMinus = (double[])angles.Clone();
                anglesMinus[i] -= delta;
                var (posMinus, oriMinus) = ForwardKinematics(joints, anglesMinus);

                J[0, i] = (posPlus.X - posMinus.X) / (2 * delta);
                J[1, i] = (posPlus.Y - posMinus.Y) / (2 * delta);
                J[2, i] = (posPlus.Z - posMinus.Z) / (2 * delta);
                J[3, i] = (oriPlus.X - oriMinus.X) * Math.PI / 180.0 / (2 * delta); // Chuyển độ sang radian
                J[4, i] = (oriPlus.Y - oriMinus.Y) * Math.PI / 180.0 / (2 * delta);
                J[5, i] = (oriPlus.Z - oriMinus.Z) * Math.PI / 180.0 / (2 * delta);
            }

            return J;
        }

        public double DistanceFromTarget(IList<Joint> joints, Vector3D targetPosition,
            Vector3D targetOrientation, double[] angles)
        {
            var (position, orientation) = ForwardKinematics(joints, angles);
            var error = Vector<double>.Build.DenseOfArray(new[]
            {
                position.X - targetPosition.X,
                position.Y - targetPosition.Y,
                position.Z - targetPosition.Z,
                (orientation.X - targetOrientation.X) * Math.PI / 180.0, // Chuyển độ sang radian
                (orientation.Y - targetOrientation.Y) * Math.PI / 180.0,
                (orientation.Z - targetOrientation.Z) * Math.PI / 180.0
            });
            return error.L2Norm();
        }

        private Vector3D ExtractEulerAngles(Matrix3D matrix)
        {
            double roll, pitch, yaw;
            double sy = Math.Sqrt(matrix.M11 * matrix.M11 + matrix.M21 * matrix.M21);
            bool singular = sy < 1e-6;

            if (!singular)
            {
                roll = Math.Atan2(matrix.M32, matrix.M33);
                pitch = Math.Atan2(-matrix.M31, sy);
                yaw = Math.Atan2(matrix.M21, matrix.M11);
            }
            else
            {
                roll = Math.Atan2(-matrix.M23, matrix.M22);
                pitch = Math.Atan2(-matrix.M31, sy);
                yaw = 0;
            }

            return new Vector3D(
                roll * 180.0 / Math.PI,
                pitch * 180.0 / Math.PI,
                yaw * 180.0 / Math.PI
            );
        }

        private double AngleDifference(double angle1, double angle2)
        {
            double diff = angle1 - angle2;
            while (diff > 180) diff -= 360;
            while (diff < -180) diff += 360;
            return Math.Abs(diff);
        }
    }
}
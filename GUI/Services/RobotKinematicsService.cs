using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using GUI.Models;

namespace GUI.Services
{
    public interface IRobotKinematicsService
    {
        Vector3D ForwardKinematics(IList<Joint> joints, double[] angles);
        double[] InverseKinematics(IList<Joint> joints, Vector3D target, double[] currentAngles, int maxIterations = 5000);
        double DistanceFromTarget(IList<Joint> joints, Vector3D target, double[] angles);
    }

    public class RobotKinematicsService : IRobotKinematicsService
    {
        private const double LearningRate = 0.01;
        private const double SamplingDistance = 0.15;
        private const double DistanceThreshold = 20;

        public Vector3D ForwardKinematics(IList<Joint> joints, double[] angles)
        {
            if (joints == null || joints.Count < 6 || angles.Length < 6)
                return new Vector3D(0, 0, 0);

            var transforms = new Transform3DGroup[6];

            for (int i = 0; i < 6; i++)
            {
                transforms[i] = new Transform3DGroup();
                transforms[i].Children.Add(new TranslateTransform3D(0, 0, 0));

                var rotation = new RotateTransform3D(
                    new AxisAngleRotation3D(joints[i].RotationAxis, angles[i]),
                    joints[i].RotationPoint
                );
                transforms[i].Children.Add(rotation);

                if (i > 0)
                {
                    transforms[i].Children.Add(transforms[i - 1]);
                }

                if (joints[i].Model != null)
                {
                    joints[i].Model.Transform = transforms[i];
                }
            }

            var bounds = joints[5].Model?.Bounds ?? new Rect3D(0, 0, 0, 0, 0, 0);
            return new Vector3D(bounds.Location.X, bounds.Location.Y, bounds.Location.Z);
        }

        public double[] InverseKinematics(IList<Joint> joints, Vector3D target, double[] angles, int maxIterations = 5000)
        {
            double[] newAngles = new double[6];
            angles.CopyTo(newAngles, 0);

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                if (DistanceFromTarget(joints, target, newAngles) < DistanceThreshold)
                {
                    break;
                }

                double[] oldAngles = (double[])newAngles.Clone();

                for (int i = 0; i < 6; i++)
                {
                    double gradient = PartialGradient(joints, target, newAngles, i);
                    newAngles[i] -= LearningRate * gradient;
                    newAngles[i] = Math.Clamp(newAngles[i], joints[i].AngleMin, joints[i].AngleMax);
                }

                if (AnglesEqual(oldAngles, newAngles))
                {
                    break;
                }
            }

            return newAngles;
        }

        public double DistanceFromTarget(IList<Joint> joints, Vector3D target, double[] angles)
        {
            Vector3D position = ForwardKinematics(joints, angles);
            return Math.Sqrt(
                Math.Pow(position.X - target.X, 2) +
                Math.Pow(position.Y - target.Y, 2) +
                Math.Pow(position.Z - target.Z, 2)
            );
        }

        private double PartialGradient(IList<Joint> joints, Vector3D target, double[] angles, int jointIndex)
        {
            double originalAngle = angles[jointIndex];
            double fx = DistanceFromTarget(joints, target, angles);

            angles[jointIndex] += SamplingDistance;
            double fxh = DistanceFromTarget(joints, target, angles);

            double gradient = (fxh - fx) / SamplingDistance;
            angles[jointIndex] = originalAngle;

            return gradient;
        }

        private bool AnglesEqual(double[] angles1, double[] angles2)
        {
            for (int i = 0; i < 6; i++)
            {
                if (Math.Abs(angles1[i] - angles2[i]) > 0.001)
                    return false;
            }
            return true;
        }
    }
}
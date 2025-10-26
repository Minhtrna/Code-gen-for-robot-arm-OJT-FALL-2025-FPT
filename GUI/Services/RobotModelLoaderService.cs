using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using GUI.Models;

namespace GUI.Services
{
    public interface IRobotModelLoaderService
    {
        List<Joint> LoadRobotModels(RobotType robotType, string basePath);
        Model3DGroup GetAllModels(List<Joint> joints, RobotType robotType);
    }

    public class RobotModelLoaderService : IRobotModelLoaderService
    {
        private List<Model3D> _allModels = new List<Model3D>();

        public List<Joint> LoadRobotModels(RobotType robotType, string basePath)
        {
            var config = RobotConfiguration.GetConfiguration(robotType);
            var joints = new List<Joint>();
            var importer = new ModelImporter();

            // Map RobotType to folder name
            string modelFolder = robotType switch
            {
                RobotType.IRB6700 => "IRB6700",
                RobotType.IRB4600 => "IRB4600",
                RobotType.AUBO_I10 => "AUBO_I10",
                _ => "IRB6700"
            };

            string fullPath = Path.Combine(basePath, modelFolder);

            // Debug: Show path being used
            if (!Directory.Exists(fullPath))
            {
                MessageBox.Show(
                    $"Model folder not found:\n{fullPath}\n\nPlease check the path!",
                    "Path Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return joints;
            }

            _allModels.Clear();

            for (int i = 0; i < config.ModelPaths.Count; i++)
            {
                try
                {
                    string modelPath = Path.Combine(fullPath, config.ModelPaths[i]);

                    if (!File.Exists(modelPath))
                    {
                        continue;
                    }

                    var model = importer.Load(modelPath);
                    _allModels.Add(model);

                    SetModelMaterial(model, Colors.White);

                    if (i < 6)
                    {
                        var joint = new Joint(i)
                        {
                            Model = model
                        };

                        var jointConfig = config.JointConfigs[i];
                        joint.AngleMin = jointConfig.AngleMin;
                        joint.AngleMax = jointConfig.AngleMax;
                        joint.RotAxisX = jointConfig.RotAxis.X;
                        joint.RotAxisY = jointConfig.RotAxis.Y;
                        joint.RotAxisZ = jointConfig.RotAxis.Z;
                        joint.RotPointX = jointConfig.RotPoint.X;
                        joint.RotPointY = jointConfig.RotPoint.Y;
                        joint.RotPointZ = jointConfig.RotPoint.Z;

                        joints.Add(joint);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading model {config.ModelPaths[i]}: {ex.Message}",
                        "Model Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            return joints;
        }

        public Model3DGroup GetAllModels(List<Joint> joints, RobotType robotType)
        {
            var group = new Model3DGroup();

            foreach (var joint in joints)
            {
                if (joint.Model != null)
                    group.Children.Add(joint.Model);
            }

            for (int i = 6; i < _allModels.Count; i++)
            {
                group.Children.Add(_allModels[i]);

                if (robotType == RobotType.IRB6700)
                {
                    if (i >= 6 && i <= 13)
                        SetModelMaterial(_allModels[i], Colors.DarkSlateGray);
                    else if (i == 14)
                        SetModelMaterial(_allModels[i], Colors.Gray);
                    else if (i >= 15 && i <= 17)
                        SetModelMaterial(_allModels[i], Colors.Red);
                    else if (i >= 18)
                        SetModelMaterial(_allModels[i], Colors.Gray);
                }
                else
                {
                    if (i == 6)
                        SetModelMaterial(_allModels[i], Colors.Red);
                    else if (i >= 7 && i <= 9)
                        SetModelMaterial(_allModels[i], Colors.Black);
                    else if (i == 10)
                        SetModelMaterial(_allModels[i], Colors.Gray);
                }
            }

            return group;
        }

        private void SetModelMaterial(Model3D model, Color color)
        {
            if (model is Model3DGroup group)
            {
                foreach (var child in group.Children)
                {
                    SetModelMaterial(child, color);
                }
            }
            else if (model is GeometryModel3D geomModel)
            {
                var materialGroup = new MaterialGroup();
                materialGroup.Children.Add(new EmissiveMaterial(new SolidColorBrush(color)));
                materialGroup.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                materialGroup.Children.Add(new SpecularMaterial(new SolidColorBrush(color), 200));

                geomModel.Material = materialGroup;
                geomModel.BackMaterial = materialGroup;
            }
        }
    }
}
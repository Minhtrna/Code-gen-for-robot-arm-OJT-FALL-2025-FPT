using System.Collections.Generic;

namespace GUI.Models
{
    public class RobotConfiguration
    {
        public RobotType Type { get; set; }
        public List<string> ModelPaths { get; set; }
        public List<JointConfig> JointConfigs { get; set; }

        public static RobotConfiguration GetConfiguration(RobotType type)
        {
            return type == RobotType.IRB6700 ? GetIRB6700Config() : type == RobotType.IRB4600 ? GetIRB4600Config() : GetAUBOI10Config();
        }

        private static RobotConfiguration GetIRB6700Config()
        {
            return new RobotConfiguration
            {
                Type = RobotType.IRB6700,
                ModelPaths = new List<string>
                {
                    "IRB6700-MH3_245-300_IRC5_rev02_LINK01_CAD.stl",
                    "IRB6700-MH3_245-300_IRC5_rev00_LINK02_CAD.stl",
                    "IRB6700-MH3_245-300_IRC5_rev02_LINK03_CAD.stl",
                    "IRB6700-MH3_245-300_IRC5_rev01_LINK04_CAD.stl",
                    "IRB6700-MH3_245-300_IRC5_rev01_LINK05_CAD.stl",
                    "IRB6700-MH3_245-300_IRC5_rev01_LINK06_CAD.stl",
                    "IRB6700-MH3_245-300_IRC5_rev00_ROD_CAD.stl",
                    "IRB6700-MH3_245-300_IRC5_rev00_LOGO1_CAD.stl",
                    "IRB6700-MH3_245-300_IRC5_rev00_LOGO2_CAD.stl",
                    "IRB6700-MH3_245-300_IRC5_rev00_LOGO3_CAD.stl",
                    "IRB6700-MH3_245-300_IRC5_rev01_BASE_CAD.stl",
                    "IRB6700-MH3_245-300_IRC5_rev00_CYLINDER_CAD.stl"
                },
                JointConfigs = new List<JointConfig>
                {
                    new JointConfig { Index = 0, AngleMin = -180, AngleMax = 180,
                        RotAxis = (0, 0, 1), RotPoint = (0, 0, 0) },
                    new JointConfig { Index = 1, AngleMin = -100, AngleMax = 60,
                        RotAxis = (0, 1, 0), RotPoint = (348, -243, 775) },
                    new JointConfig { Index = 2, AngleMin = -90, AngleMax = 90,
                        RotAxis = (0, 1, 0), RotPoint = (347, -376, 1923) },
                    new JointConfig { Index = 3, AngleMin = -180, AngleMax = 180,
                        RotAxis = (1, 0, 0), RotPoint = (60, 0, 2125) },
                    new JointConfig { Index = 4, AngleMin = -115, AngleMax = 115,
                        RotAxis = (0, 1, 0), RotPoint = (1815, 0, 2125) },
                    new JointConfig { Index = 5, AngleMin = -180, AngleMax = 180,
                        RotAxis = (1, 0, 0), RotPoint = (2008, 0, 2125) }
                }
            };
        }

        private static RobotConfiguration GetIRB4600Config()
        {
            return new RobotConfiguration
            {
                Type = RobotType.IRB4600,
                ModelPaths = new List<string>
                {
                    "IRB4600_20kg-250_LINK1_CAD_rev04.stl",
                    "IRB4600_20kg-250_LINK2_CAD_rev04.stl",
                    "IRB4600_20kg-250_LINK3_CAD_rev005.stl",
                    "IRB4600_20kg-250_LINK4_CAD_rev04.stl",
                    "IRB4600_20kg-250_LINK5_CAD_rev04.stl",
                    "IRB4600_20kg-250_LINK6_CAD_rev04.stl",
                    "IRB4600_20kg-250_LINK3_CAD_rev04.stl",
                    "IRB4600_20kg-250_BASE_CAD_rev04.stl"
                },
                JointConfigs = new List<JointConfig>
                {
                    new JointConfig { Index = 0, AngleMin = -180, AngleMax = 180,
                        RotAxis = (0, 0, 1), RotPoint = (0, 0, 0) },
                    new JointConfig { Index = 1, AngleMin = -100, AngleMax = 60,
                        RotAxis = (0, 1, 0), RotPoint = (175, -200, 500) },
                    new JointConfig { Index = 2, AngleMin = -90, AngleMax = 90,
                        RotAxis = (0, 1, 0), RotPoint = (190, -700, 1595) },
                    new JointConfig { Index = 3, AngleMin = -180, AngleMax = 180,
                        RotAxis = (1, 0, 0), RotPoint = (400, 0, 1765) },
                    new JointConfig { Index = 4, AngleMin = -115, AngleMax = 115,
                        RotAxis = (0, 1, 0), RotPoint = (1405, 50, 1765) },
                    new JointConfig { Index = 5, AngleMin = -180, AngleMax = 180,
                        RotAxis = (1, 0, 0), RotPoint = (1405, 0, 1765) }
                }

            };
        }

        private static RobotConfiguration GetAUBOI10Config()
        {
            return new RobotConfiguration
            {
                Type = RobotType.AUBO_I10,
                ModelPaths = new List<string>
                {
                    "I10i10_arm_1.stl",
                    "I10i10_arm_2.stl",
                    "I10i10_base.stl",
                    "I10Mesh.001.stl",
                    "I10Mesh.002.stl",
                    "I10Mesh.003.stl",
                    "I10Mesh.004.stl",
                    "I10Mesh.005.stl",
                    "I10Mesh.006.stl",
                    "I10Mesh.007.stl",
                    "I10Mesh.008.stl",
                    "I10Mesh.009.stl",
                    "I10Mesh.010.stl",
                    "I10Mesh.011.stl",
                    "I10Mesh.012.stl",
                    "I10Mesh.013.stl",
                    "I10Mesh.014.stl",
                    "I10Mesh.015.stl",
                    "I10Mesh.016.stl",
                    "I10Mesh.017.stl",
                    "I10Mesh.018.stl",
                    "I10Mesh.019.stl",
                    "I10Mesh.020.stl",
                    "I10Mesh.021.stl",
                    "I10Mesh.022.stl",
                    "I10Mesh.023.stl",
                    "I10Mesh.024.stl",
                    "I10Mesh.025.stl",
                    "I10Mesh.026.stl",
                    "I10Mesh.027.stl",
                    "I10Mesh.028.stl",
                    "I10Mesh.029.stl",
                    "I10Mesh.030.stl",
                    "I10Mesh.031.stl",
                    "I10Mesh.032.stl",
                    "I10Mesh.stl"
                    
                },
                JointConfigs = new List<JointConfig>
                {
                    new JointConfig { Index = 0, AngleMin = -180, AngleMax = 180,
                        RotAxis = (0, 0, 1), RotPoint = (0, 0, 0) },
                    new JointConfig { Index = 1, AngleMin = -100, AngleMax = 60,
                        RotAxis = (0, 1, 0), RotPoint = (175, -200, 500) },
                    new JointConfig { Index = 2, AngleMin = -90, AngleMax = 90,
                        RotAxis = (0, 1, 0), RotPoint = (190, -700, 1595) },
                    new JointConfig { Index = 3, AngleMin = -180, AngleMax = 180,
                        RotAxis = (1, 0, 0), RotPoint = (400, 0, 1765) },
                    new JointConfig { Index = 4, AngleMin = -115, AngleMax = 115,
                        RotAxis = (0, 1, 0), RotPoint = (1405, 50, 1765) },
                    new JointConfig { Index = 5, AngleMin = -180, AngleMax = 180,
                        RotAxis = (1, 0, 0), RotPoint = (1405, 0, 1765) }
                }

            };
        }
    }

    public class JointConfig
    {
        public int Index { get; set; }
        public double AngleMin { get; set; }
        public double AngleMax { get; set; }
        public (int X, int Y, int Z) RotAxis { get; set; }
        public (int X, int Y, int Z) RotPoint { get; set; }
    }
}
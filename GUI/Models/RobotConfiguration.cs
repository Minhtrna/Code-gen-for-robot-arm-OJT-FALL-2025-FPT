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
            return type == RobotType.IRB6700 ? GetIRB6700Config() : GetIRB4600Config();
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
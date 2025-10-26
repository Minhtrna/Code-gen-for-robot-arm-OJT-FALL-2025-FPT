using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Media3D;

namespace GUI.Models
{
    
    public class Joint : INotifyPropertyChanged
    {
        private double _angle;
        private int _rotPointX;
        private int _rotPointY;
        private int _rotPointZ;

        public int Index { get; set; }
        public Model3D Model { get; set; }

        
        public double Angle
        {
            get => _angle;
            set
            {
                if (Math.Abs(_angle - value) > 0.001)
                {
                    _angle = Math.Clamp(value, AngleMin, AngleMax);
                    OnPropertyChanged();
                }
            }
        }

        public double AngleMin { get; set; } = -180;
        public double AngleMax { get; set; } = 180;

        // Rotation Point 
        public int RotPointX
        {
            get => _rotPointX;
            set
            {
                _rotPointX = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RotationPoint));
            }
        }

        public int RotPointY
        {
            get => _rotPointY;
            set
            {
                _rotPointY = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RotationPoint));
            }
        }

        public int RotPointZ
        {
            get => _rotPointZ;
            set
            {
                _rotPointZ = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RotationPoint));
            }
        }

        public Point3D RotationPoint => new Point3D(RotPointX, RotPointY, RotPointZ);

        // Rotation Axis 
        public int RotAxisX { get; set; }
        public int RotAxisY { get; set; }
        public int RotAxisZ { get; set; }
        public Vector3D RotationAxis => new Vector3D(RotAxisX, RotAxisY, RotAxisZ);

        public Joint(int index)
        {
            Index = index;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
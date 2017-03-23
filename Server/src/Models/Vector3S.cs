using GTANetworkShared;
using System;

namespace GTAIdentity.Models
{
    [Serializable]
    public class Vector3S
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        protected Vector3S() { }
        public Vector3S(float x,float y,float z)
        {
            X = x; Y = y; Z = z;
        }
        public Vector3S(Vector3 vec)
        {
            X = vec.X;
            Y = vec.Y;
            Z = vec.Z;
        }
        public Vector3S(Vector3S vec)
        {
            X = vec.X;
            Y = vec.Y;
            Z = vec.Z;
        }

        public static implicit operator Vector3(Vector3S v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }
        public static implicit operator Vector3S(Vector3 v)
        {
            return new Vector3S(v.X, v.Y, v.Z);
        }

        public override bool Equals(object obj)
        {
            var vec = obj as Vector3S;
            if (vec == null)
                return false;
            return vec.X == X && vec.Y == Y && vec.Z == Z;
        }

        public override int GetHashCode()
        {
            return (17 * (X.GetHashCode()) * 23 + Y.GetHashCode()) * 23 + Z.GetHashCode();
        }
    }
}

using System;
using UnityEngine;

namespace PathFinder3D
{
    public class Cube : IVertex
    {
        Cube goal;
        public Vector3 index;

        public Cube(Vector3 index)
        {
            this.index = index;
            goal = this;
        }

        public Cube(Vector3 index, Cube goal)
        {
            this.index = index;
            this.goal = goal;
        }

        public double HFunction()
        {
            double dx = goal.index.x - index.x;
            double dy = goal.index.y - index.y;
            double dz = goal.index.z - index.z;
            double distsqr = Math.Pow(dx, 2) + Math.Pow(dy, 2) + Math.Pow(dz, 2);
            return Math.Sqrt(distsqr);
        }

        public override bool Equals(object otherAbstr)
        {
            Cube other = (Cube)otherAbstr;
            return index.Equals(other.index);
        }
    }
}

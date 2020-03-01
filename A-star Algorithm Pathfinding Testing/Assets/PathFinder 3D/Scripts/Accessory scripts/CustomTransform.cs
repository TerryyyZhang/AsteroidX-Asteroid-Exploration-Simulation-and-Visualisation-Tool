using UnityEngine;

namespace PathFinder3D
{
    public class CustomTransform
    {
        public CustomTransform()
        {
            position = new positionClass();
            localScale = new localScaleClass();
        }
        public class positionClass
        {
            public float x;
            public float y;
            public float z;

            public static implicit operator Vector3(positionClass param)
            {
                return new Vector3(param.x, param.y, param.z);
            }
        };
        public class localScaleClass
        {
            public float x;
            public float y;
            public float z;

            public static implicit operator Vector3(localScaleClass param)
            {
                return new Vector3(param.x, param.y, param.z);
            }
        };
        public class rotationClass
        {
            public float x;
            public float y;
            public float z;
        };

        public positionClass position;
        public localScaleClass localScale;
        public Quaternion rotation;

        public static implicit operator CustomTransform(Transform param)
        {
            if (param == null) return null;

            CustomTransform newInstance = new CustomTransform();
        
            newInstance.position.x = param.position.x;
            newInstance.position.y = param.position.y;
            newInstance.position.z = param.position.z;
            newInstance.localScale.x = param.lossyScale.x;
            newInstance.localScale.y = param.lossyScale.y;
            newInstance.localScale.z = param.lossyScale.z;

            newInstance.rotation = param.rotation;

            return newInstance;
        }
    }
}

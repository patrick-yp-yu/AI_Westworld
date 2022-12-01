using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Response
{
    public string Text;
    public ObjectManager Object;

    [System.Serializable]
    public class ObjectManager {
        public List<NewObj> Create;
        public List<string> Delete;

        [System.Serializable]
        public class NewObj {
            public string Name;
            public string Type;
            public string Color;
            //public string Size;
            public float Size;
            public float[] Location;
        }
    }
}

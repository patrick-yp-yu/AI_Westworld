using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Response
{
    public string Text; // Text to be displayed as response
    public ObjectManager Object; // All objects to be created or deleted

    [System.Serializable]
    public class ObjectManager {
        public List<NewObj> Create; // List of all objects to be created
        public List<string> Delete; // List of all names of objects to be deleted

        [System.Serializable]
        public class NewObj {
            public string Name; // Name of object to be created
            public string Type; // Type of object to be created
            public string Color; // Color of object to be created
            public float Size; // Size of object to be created, limited to height = length = width
            public float[] Location; // Location of the object to be created, in the form of [height, distance, angle]
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text.RegularExpressions;
using System.IO;

public class ObjectGeneration
{
    public Dictionary <string,GameObject> objectsDict;
    GameObject player;

    // Dictionaries to translate string into appropriate type or color object
    private static readonly Dictionary<string, PrimitiveType> TypeDict = new Dictionary<string, PrimitiveType> {
        {"sphere", PrimitiveType.Sphere},
        {"cube", PrimitiveType.Cube},
        {"cylinder", PrimitiveType.Cylinder},
        {"capsule", PrimitiveType.Capsule},
    };
    private static readonly Dictionary<string, Color> ColorDict = new Dictionary<string, Color> {
        {"black", Color.black},
        {"blue", Color.blue},
        {"cyan", Color.cyan},
        {"gray", Color.gray},
        {"green", Color.green},
        {"magenta", Color.magenta},
        {"red", Color.red},
        {"white", Color.white},
        {"yellow", Color.yellow},
    };

    // Deprecated dictionary for size, now handled on server side
    /*
    private static readonly Dictionary<string, Vector3> SizeDict = new Dictionary<string, Vector3> {
        {"small", new Vector3 (0.5f, 0.5f, 0.5f)},
        {"medium", new Vector3 (1.5f, 1.5f, 1.5f)},
        {"large", new Vector3 (3.0f, 3.0f, 3.0f)}
    };
    */

    // Instatiating object dictionary and finding player object
    public ObjectGeneration() {
        objectsDict = new Dictionary<string,GameObject>();
        player = GameObject.Find("Player");
    }

    // Loading prefab via filename from "Resources" folder in Assets to instantiate as new GameObject
    private UnityEngine.Object LoadPrefabFromFile(string filename)
    {
        Debug.Log("Trying to load LevelPrefab from file ("+filename+ ")...");
        var loadedObject = Resources.Load(filename);
        if (loadedObject == null)
        {
            throw new FileNotFoundException("...no file found - please check the configuration");
        }
        return loadedObject;
    }

    // Parsing object manager response to create and delete objects
    public void PickPrefab(Response.ObjectManager objectManager) {
        // Delete designated objects from unity scene and object dictionary 
        foreach (string objName in objectManager.Delete) {
            GameObject objToRemove;
            if (objectsDict.TryGetValue(objName, out objToRemove))
            {
                GameObject.Destroy(objToRemove);
                objectsDict.Remove(objName);
            }
        }

        // Create designated objects with type, color, size, and location attributes
        foreach (Response.ObjectManager.NewObj obj in objectManager.Create) {
            // Calculating radian direction relative to current player perspective
            var newDirectionRad = ((player.transform.eulerAngles.y + obj.Location[2])*Math.PI) / 180; 
            // Calculating XYZ coordinates of new object's location
            var newX = obj.Location[1]*Math.Sin(newDirectionRad);
            var newY = obj.Location[0];
            var newZ = obj.Location[1]*Math.Cos(newDirectionRad);
            var newVector3Pos = player.transform.position + new Vector3((float)newX, (float)newY/4.0f, (float)newZ);

            // Loading prefab of designated type from "Resources" folder in Assets to instantiate as new GameObject
            var loadedPrefabResource = LoadPrefabFromFile(obj.Type);
            GameObject instancedObj = GameObject.Instantiate(loadedPrefabResource, newVector3Pos, Quaternion.identity) as GameObject;

            // Add rigidbody and mesh collider components to new GameObject to apply gravity
            Rigidbody newRigidbody = instancedObj.AddComponent<Rigidbody>();
            newRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            MeshCollider newMeshCollider = instancedObj.AddComponent<MeshCollider>();
            newMeshCollider.convex = true;

            // Resize object with designated size
            instancedObj.transform.localScale = new Vector3 (obj.Size, obj.Size, obj.Size);

            // Rename object with designated name
            instancedObj.name = obj.Name;

            // Apply tint with designated color through application of new generated material with standard shader
            if (obj.Color != "default") { // format later
                Material[] mats = new Material[2];
                mats[0] = instancedObj.GetComponent<Renderer>().material;
                Material myNewMaterial = new Material(Shader.Find("Standard"));
                myNewMaterial.SetFloat("_Mode", 3);
                myNewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                myNewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                myNewMaterial.SetInt("_ZWrite", 0);
                myNewMaterial.DisableKeyword("_ALPHATEST_ON");
                myNewMaterial.DisableKeyword("_ALPHABLEND_ON");
                myNewMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                myNewMaterial.renderQueue = 3000;
            
                Color col = ColorDict[obj.Color];
                col.a *= 0.5f;
                myNewMaterial.SetColor("_Color", col);

                mats[1] = myNewMaterial;
                instancedObj.GetComponent<Renderer>().materials = mats;
            }

            // Add created object to object dictionary
            try {
                objectsDict.Add(obj.Name, instancedObj);
            } catch (ArgumentException) {
                Console.WriteLine("Key already exists!");
            }
        }
    }

    // Deprecated function originally used for generating primative objects within Unity (like spheres and cubes) instead of prefabs
    public void PickObject(Response.ObjectManager objectManager) {
        foreach (string objName in objectManager.Delete) {
            objectsDict.Remove(objName);
        }
        foreach (Response.ObjectManager.NewObj obj in objectManager.Create) {
            GameObject newObj = null;
            // Creating New Object 
            newObj = GameObject.CreatePrimitive(TypeDict[obj.Type]); 

            Rigidbody newRigidbody = newObj.AddComponent<Rigidbody>();
            newRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            MeshCollider newMeshCollider = newObj.AddComponent<MeshCollider>();
            newMeshCollider.convex = true;

            if (obj.Color != "default") {
                newObj.GetComponent<Renderer>().material.SetColor("_Color", ColorDict[obj.Color]);
            }
            
            newObj.transform.localScale = new Vector3 (obj.Size, obj.Size, obj.Size);

            var newDirectionRad = ((player.transform.eulerAngles.y + obj.Location[2])*Math.PI) / 180;
            var newX = obj.Location[1]*Math.Sin(newDirectionRad);
            var newY = obj.Location[0];
            var newZ = obj.Location[1]*Math.Cos(newDirectionRad);
            
            newObj.transform.position = player.transform.position + new Vector3((float)newX, (float)newY, (float)newZ);
            newObj.name = obj.Name;

            try {
                objectsDict.Add(obj.Name, newObj);
            } catch (ArgumentException) {
                Console.WriteLine("Key already exists!");
            }
        }
    }
}

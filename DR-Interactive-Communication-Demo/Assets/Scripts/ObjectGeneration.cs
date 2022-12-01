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
    /*
    private static readonly Dictionary<string, Vector3> SizeDict = new Dictionary<string, Vector3> {
        {"small", new Vector3 (0.5f, 0.5f, 0.5f)},
        {"medium", new Vector3 (1.5f, 1.5f, 1.5f)},
        {"large", new Vector3 (3.0f, 3.0f, 3.0f)}
    };
    */
    public ObjectGeneration() {
        objectsDict = new Dictionary<string,GameObject>();
        player = GameObject.Find("Player");
    }

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

            if (obj.Color != "default") { // format later
                newObj.GetComponent<Renderer>().material.SetColor("_Color", ColorDict[obj.Color]);
            }
            
            //newObj.transform.localScale = SizeDict[obj.Size];
            newObj.transform.localScale = new Vector3 (obj.Size, obj.Size, obj.Size);

            var newDirectionRad = ((player.transform.eulerAngles.y + obj.Location[2])*Math.PI) / 180;
            var newX = obj.Location[1]*Math.Sin(newDirectionRad);
            var newY = obj.Location[0];
            var newZ = obj.Location[1]*Math.Cos(newDirectionRad);
            
            newObj.transform.position = player.transform.position + new Vector3((float)newX, (float)newY, (float)newZ);
            //newObj.transform.position = new Vector3(UnityEngine.Random.Range(-20.0f, 20.0f), 1.5f, UnityEngine.Random.Range(-20.0f, 20.0f));
            newObj.name = obj.Name;

            try {
                objectsDict.Add(obj.Name, newObj);
            } catch (ArgumentException) {
                Console.WriteLine("Key already exists!");
            }
        }
    }

    public void PickAnimal(Response.ObjectManager objectManager) {
        foreach (string objName in objectManager.Delete) {
            GameObject objToRemove;
            if (objectsDict.TryGetValue(objName, out objToRemove))
            {
                GameObject.Destroy(objToRemove);
                objectsDict.Remove(objName);
            }
        }

        foreach (Response.ObjectManager.NewObj obj in objectManager.Create) {
            var newDirectionRad = ((player.transform.eulerAngles.y + obj.Location[2])*Math.PI) / 180;
            var newX = obj.Location[1]*Math.Sin(newDirectionRad);
            var newY = obj.Location[0];
            var newZ = obj.Location[1]*Math.Cos(newDirectionRad);
            var newVector3Pos = player.transform.position + new Vector3((float)newX, (float)newY/4.0f, (float)newZ);
            
            var loadedPrefabResource = LoadPrefabFromFile(obj.Type);
            GameObject instancedObj = GameObject.Instantiate(loadedPrefabResource, newVector3Pos, Quaternion.identity) as GameObject;

            Rigidbody newRigidbody = instancedObj.AddComponent<Rigidbody>();
            newRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            MeshCollider newMeshCollider = instancedObj.AddComponent<MeshCollider>();
            newMeshCollider.convex = true;

            instancedObj.transform.localScale = new Vector3 (obj.Size, obj.Size, obj.Size);
            instancedObj.name = obj.Name;

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

            try {
                objectsDict.Add(obj.Name, instancedObj);
            } catch (ArgumentException) {
                Console.WriteLine("Key already exists!");
            }
        }
    }

}
    /*
            // Type
            if (obj.Type == "sphere") {
                newObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            } else if (obj.Type == "cube") {
                newObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }

            // Color
            if (obj.Color == "red") {
                newObj.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
            } else if (obj.Color == "blue") {
                newObj.GetComponent<Renderer>().material.SetColor("_Color", Color.blue);
            } else if (obj.Color == "green") {
                newObj.GetComponent<Renderer>().material.SetColor("_Color", Color.green);
            } else if (obj.Color == "white") {
                newObj.GetComponent<Renderer>().material.SetColor("_Color", Color.white);
            }

            // Size
            if (obj.Size == "small") {
                newObj.transform.localScale = new Vector3 (0.5f, 0.5f, 0.5f);
            } else if (obj.Size == "medium") {
                newObj.transform.localScale = new Vector3 (1.5f, 1.5f, 1.5f);
            } else if (obj.Size == "large") {
                newObj.transform.localScale = new Vector3 (3.0f, 3.0f, 3.0f);
            } 
            
            // Location
            newObj.transform.position = new Vector3(1.5f, 1.5f, 1.5f); // to remove later
            if (obj.Location == "default") {
                newObj.transform.position = new Vector3(UnityEngine.Random.Range(-20.0f, 20.0f), 1.5f, UnityEngine.Random.Range(-20.0f, 20.0f));
            }

            // Name
            newObj.name = obj.Name;
            Debug.Log("NAME: "+obj.Name);
    */

        /*
        Regex rgx = new Regex("[^a-zA-Z0-9 ]");
        string processedMessage = rgx.Replace(messageText.Substring(5), "");
        HashSet<string> words = new HashSet<string>();
        foreach (string w in processedMessage.Split()) {
            words.Add(w.ToLower());
        }
        Debug.Log(processedMessage);
        GameObject newObj = null;
        if (words.Contains("sphere") || words.Contains("ball")) {
            Debug.Log("ball detected");
            newObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            objects.Add(newObj);
        } else if (words.Contains("cube") || words.Contains("box")) {
            newObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            objects.Add(newObj);
        } else if (words.Contains("delete") || words.Contains("remove")){            
            newObj = null;
            for (int i = 0; i < objects.Count; i++) {
                Destroy(objects[i].gameObject);
            }
            objects.Clear();
        }  
        
        if (newObj != null) {
            float x = 1.5f;
            float z = 0.0f;
            if (messageText.Length > 14) {
                if (messageText[14] >= 65) {
                    x = (float)Char.ToLower(messageText[14])-110.0f;
                }
                if (messageText[14] >= 65) {
                    z = (float)Char.ToLower(messageText[9])-110.0f;
                }
            }
            newObj.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
            newObj.transform.position = new Vector3(x, 1.5f, z);
        }
        */

    /*
    private GameObject CreateSphere() 
    {
        return GameObject.CreatePrimitive(PrimitiveType.Sphere);
    }

    private GameObject CreateCube() 
    {
        return GameObject.CreatePrimitive(PrimitiveType.Cube);
    }
    */

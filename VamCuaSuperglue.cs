using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class VamCuaSuperglue : MVRScript
{
    private JSONStorableBool _myValueJSON;
    public List<Atom> cuaAtoms;
    public List<Pose> prevFrame;
    public List<Pose> thisFrame;

    public override void Init() {

        // Example storable; you can also create string, float and action JSON storables
        _myValueJSON = new JSONStorableBool("My Storable", false);
        // You can listen to changes
        _myValueJSON.setCallbackFunction = (bool val) => SuperController.LogMessage($"VamCuaSuperglue: Received {val}");
        // You can use Register* methods to make your storable triggerable, and save and restore the value with the scene
        RegisterBool(_myValueJSON);
        // You can use Create* methods to add a control in the plugin's custom UI
        CreateToggle(_myValueJSON);

        RefreshCuas();

    }


    public void RefreshCuas() {
        // TODO: Filter only atoms with "Parent Link"
        cuaAtoms = SuperController.singleton.GetAtoms()
            .FindAll(a => a.type.Equals("CustomUnityAsset"));

        prevFrame = new List<Pose>(cuaAtoms.Count);
        thisFrame = new List<Pose>(cuaAtoms.Count);

        foreach (Atom a in cuaAtoms) {
            Transform reparent = a.transform.Find("reParentObject/object");
            if (reparent == null) {
                Log(a.name + ": no control");
                continue;
            }

            Pose pose = new Pose(reparent.transform.position, reparent.transform.rotation);
            prevFrame.Add(pose);
            thisFrame.Add(pose);

            Log(a.name + " " + reparent.transform.position + " " + !a.freezePhysicsJSON.valNoCallback);
        }

        Log(ObjectHierarchyToString(cuaAtoms[0].transform, t => t.name + " " + (t.GetComponent<Rigidbody>() != null)));
    }


    public void LateUpdate() {
        for(int i = 0; i < cuaAtoms.Count; i++) {
            Transform a = cuaAtoms[i].transform.Find("reParentObject/object");
            thisFrame[i] = new Pose(a.position, a.rotation);

            Pose prevPose = prevFrame[i];
            a.position = prevPose.position;
            a.rotation = prevPose.rotation;

        }

        // swap pose buffers
        List<Pose> tempList = prevFrame;
        prevFrame = thisFrame;
        thisFrame = tempList;
    }


    #region Util
    public static void Log(string message) {
        SuperController.LogMessage(message);
    }

    public static void Err(string message) {
        SuperController.LogError(message);
    }


    /// <summary>
    /// Get a string containing a visual representation of a Transform's children hierarchy.
    /// </summary>
    /// <param name="root">The parent Transform (GameObject)</param>
    /// <param name="propertyDel">A delegate function that takes a Transform and returns the string to print as an entry in the hierarchy.
    /// For example, to print a hierarchy of each transform's name: <code>thisTransform => thisTransform.name</code> A null value will print names.</param>
    /// <returns>A string containing a visual hierarchy of all child transforms, including the root</returns>
    public static string ObjectHierarchyToString(Transform root, Func<Transform, string> propertyDel = null) {
        if (propertyDel == null) {
            propertyDel = t => t.name;
        }
        StringBuilder builder = new StringBuilder();
        ObjectHierarchyToString(root, propertyDel, builder);

        if (builder.Length < 1024) {
            return builder.ToString();
        }
        return $"Output string length {builder.Length} may be too large for viewing in VAM. See %userprofile%/AppData/LocalLow/MeshedVR/VaM/output_log.txt for the full output.\n{builder}";
    }

    private static void ObjectHierarchyToString(Transform root, Func<Transform, string> propertyDel, StringBuilder builder, int currentDepth = 0) {
        for (int i = 0; i < currentDepth; i++) {
            builder.Append("|   ");
        }

        builder.Append(propertyDel(root) + "\n");
        foreach (Transform child in root) {
            ObjectHierarchyToString(child, propertyDel, builder, currentDepth+1);
        }
    }
    #endregion
}

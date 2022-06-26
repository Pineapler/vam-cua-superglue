using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MeshVR;
using PrefabEvolution;
using UnityEngine;

public class VamCuaSuperglue : MVRScript
{
    public List<Atom> cuaAtoms;
    public List<GlueEntry> glueEntries;
    public List<Pose> prevFrame;
    public List<Pose> thisFrame;

    public override void Init() {
        // Subscribe to Atom delegates

        try {
            RefreshCuas();
        }
        catch (Exception e) {
            Err("[VamCuaSuperglue]: " + e);
            DestroyImmediate(this);
        }
    }

    public void OnDestroy() {
        // Unsubscribe from Atom delegates
    }


    public void RefreshCuas() {
        cuaAtoms = SuperController.singleton.GetAtoms()
            .FindAll(a => a.type.Equals("CustomUnityAsset") &&
                          a.mainController.linkToRB != null);


        prevFrame = new List<Pose>(cuaAtoms.Count);
        thisFrame = new List<Pose>(cuaAtoms.Count);
        glueEntries = new List<GlueEntry>(cuaAtoms.Count);

        foreach (Atom a in cuaAtoms) {
            Transform objTransform = a.transform.Find("reParentObject/object");

            Pose pose = new Pose(objTransform.position, objTransform.rotation);
            prevFrame.Add(pose);
            thisFrame.Add(pose);

            JSONStorable control = a.GetStorableByID("control");
            // Log(a.name + " " + (a.mainController.linkToRB != null));
            bool isPhysicsEnabled = control.GetBoolParamValue("physicsEnabled");
            string posStr = control.GetStringChooserParamValue("positionState");
            string rotStr = control.GetStringChooserParamValue("rotationState");
            bool isPosParentLink = posStr != null && posStr.Equals("ParentLink");
            bool isRotParentLink = rotStr != null && rotStr.Equals("ParentLink");
            glueEntries.Add(new GlueEntry(objTransform, isPhysicsEnabled, isPosParentLink, isRotParentLink));

        }
        // Log(cuaAtoms[2].GetStorableByID("control").GetAllParamAndActionNames().Aggregate( (current, next) => current + "\n    " + next));
    }

    // public void OnUIDsChanged(List<string> uids) {
    //     foreach (string s in uids) {
    //         Log(s);
    //     }
    // }


    public void LateUpdate() {
        for (int i = 0; i < glueEntries.Count; i++) {
            GlueEntry g = glueEntries[i];
            if (g.isPhysicsEnabled) {
                continue;
            }

            thisFrame[i] = new Pose(g.transform.position, g.transform.rotation);
            Pose prevPose = prevFrame[i];

            if (g.isPosParentLink) {
                g.transform.position = prevPose.position;
            }
            if (g.isRotParentLink) {
                g.transform.rotation = prevPose.rotation;
            }

        }

        // swap pose buffers
        List<Pose> tempList = prevFrame;
        prevFrame = thisFrame;
        thisFrame = tempList;
    }

    public class GlueEntry {
        // public Atom atom;
        public Transform transform;
        public bool isPhysicsEnabled;
        public bool isPosParentLink;
        public bool isRotParentLink;

        public GlueEntry(Transform transform = null, bool isPhysicsEnabled = false, bool isPosParentLink = true, bool isRotParentLink = true) {
            this.transform = transform;
            this.isPhysicsEnabled = isPhysicsEnabled;
            this.isPosParentLink = isPosParentLink;
            this.isRotParentLink = isRotParentLink;
        }
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class VamCuaSuperglue : MVRScript {
    public List<Atom> cuaAtoms;
    public Dictionary<Atom, GlueEntry> glueEntries;
    public UIDynamicTextField infoPanel;
    public UIDynamicTextField usagePanel;
    public JSONStorableString infoString;
    public JSONStorableString usageString;

    public override void Init() {
        try {
            InitCustomUI();
            SubscribeDelegates();
            RefreshCuas();
        }
        catch (Exception e) {
            Err("[VamCuaSuperglue]: " + e);
            Destroy(this);
        }
    }


    public void InitCustomUI() {
        infoString = new JSONStorableString("infoString", "Info");
        infoPanel = CreateTextField(infoString);
        infoPanel.height = 1000;

        string tmp = @"CUA Superglue by Pineapler

Usage:
This plugin should work automatically, but in the event of it not working here are some options to troubleshoot.

In your CUA, make sure the following options are selected:

Physics Object
    > Physics (Disabled)

Control
    > Link To Atom (As desired)
    > Position, Rotation (Parent Link)";

        usageString = new JSONStorableString("usageString", tmp);
        usagePanel = CreateTextField(usageString, true);
        usagePanel.height = 1000;
    }


    public void SubscribeDelegates() {
        // SuperController.singleton.onAtomUIDsChangedHandlers += OnUIDsChanged;

    }

    public void UnsubscribeDelegates() {

    }

    public void OnDestroy() {
        UnsubscribeDelegates();
    }


    public void RefreshCuas() {
        cuaAtoms = SuperController.singleton.GetAtoms()
            .FindAll(a => a.type.Equals("CustomUnityAsset"));

        glueEntries = new Dictionary<Atom, GlueEntry>(cuaAtoms.Count);

        foreach (Atom a in cuaAtoms) {
            glueEntries.Add(a, new GlueEntry(a));
        }

        UpdateInfoPanel();
    }


    public void UpdateInfoPanel() {
        infoString.val = "Glued CUAs:\n";
        StringBuilder builder = new StringBuilder();
        foreach (KeyValuePair<Atom, GlueEntry> pair in glueEntries) {
            bool isTracked = pair.Key.mainController.linkToRB != null &&
                             !pair.Value.isPhysicsEnabled;
            builder.Append(isTracked ? "\n[X]   " : "\n[   ]   ");
            builder.Append(pair.Key.name);
        }

        infoString.val += builder.ToString();
    }


    public void LateUpdate() {
        try {
            foreach (KeyValuePair<Atom, GlueEntry> pair in glueEntries) {
                pair.Value.Stick();
            }
        }
        catch (Exception e) {
            Err("[VamCuaSuperglue]: " + e);
            Destroy(this);
        }
    }

    public class GlueEntry {
        public Transform transform;
        public Pose prevPose;
        public Pose thisPose;
        public bool isPhysicsEnabled;
        public bool isPosParentLink;
        public bool isRotParentLink;

        public GlueEntry(Atom atom) {
            RefreshGlue(atom);
        }

        public void RefreshGlue(Atom atom){
            transform = atom.transform.Find("reParentObject/object");
            JSONStorable control = atom.GetStorableByID("control");

            isPhysicsEnabled = control.GetBoolParamValue("physicsEnabled");
            string posStr = control.GetStringChooserParamValue("positionState");
            string rotStr = control.GetStringChooserParamValue("rotationState");
            isPosParentLink = posStr != null && posStr.Equals("ParentLink");
            isRotParentLink = rotStr != null && rotStr.Equals("ParentLink");

            if (transform != null) {
                prevPose = new Pose(transform.position, transform.rotation);
                thisPose = prevPose;
            }

        }

        public void Stick() {
            if (isPhysicsEnabled) return;

            thisPose = new Pose(transform.position, transform.rotation);

            if (isPosParentLink) {
                transform.position = prevPose.position;
            }
            if (isRotParentLink) {
                transform.rotation = prevPose.rotation;
            }

            prevPose = thisPose;
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class VamCuaSuperglue : MVRScript {

    public static VamCuaSuperglue singleton;

    public List<Atom> cuaAtoms;
    public Dictionary<Atom, GlueEntry> glueEntries;
    public UIDynamicTextField infoPanel;
    public UIDynamicTextField usagePanel;
    public JSONStorableString infoString;
    public JSONStorableString usageString;

    private int _errCount = 0;

    public override void Init() {
        try {
            if (!containingAtom.type.Equals("SessionPluginManager") &&
                !containingAtom.type.Equals("CoreControl")) {
                Err("[VamCuaSuperglue] Please make sure this plugin is set in Session or Scene. Current: " + containingAtom.type);
                Destroy(this);
            }

            // // Singletons don't work, each script runs in its own context
            // if (singleton != null) {
            //     Err("[VamCuaSuperglue] An instance of this plugin already exists: " + singleton.containingAtom.type);
            //     Destroy(this);
            // }

            singleton = this;

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

        const string tmp = @"CUA Superglue by Pineapler

Usage:
This plugin should work automatically, but in the event of it not working here are some options to troubleshoot.

Make sure there is only one instance of this plugin loaded anywhere.

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
        SuperController.singleton.onAtomAddedHandlers += OnAtomAdded;
        SuperController.singleton.onAtomRemovedHandlers += OnAtomRemoved;
        SuperController.singleton.onSceneLoadedHandlers += RefreshCuas;
        SuperController.singleton.onSubSceneLoadedHandlers += OnSubSceneLoaded;
    }

    public void UnsubscribeDelegates() {
        SuperController.singleton.onAtomAddedHandlers -= OnAtomAdded;
        SuperController.singleton.onAtomRemovedHandlers -= OnAtomRemoved;
        SuperController.singleton.onSceneLoadedHandlers -= RefreshCuas;
        SuperController.singleton.onSubSceneLoadedHandlers -= OnSubSceneLoaded;

    }

    public void OnAtomAdded(Atom atom) {
        DbgLog("Atom added: " + atom.name);
        glueEntries.Add(atom, new GlueEntry(atom));
        UpdateInfoPanel();
    }

    public void OnAtomRemoved(Atom atom) {
        DbgLog("Atom removed: " + atom.name);
        if (glueEntries.ContainsKey(atom)) {
            glueEntries.Remove(atom);
        }
        UpdateInfoPanel();
    }

    public void OnSubSceneLoaded(SubScene subScene) {
        RefreshCuas();
    }


    public void OnDestroy() {
        UnsubscribeDelegates();
        foreach (KeyValuePair<Atom, GlueEntry> pair in glueEntries) {
            pair.Value.Unsubscribe();
        }
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
            builder.Append(pair.Value.IsTracked ? "\n[X]   " : "\n[   ]   ");
            builder.Append(pair.Key.name);
        }

        infoString.val += builder.ToString();
    }


    public void LateUpdate() {
        try {
            foreach (KeyValuePair<Atom, GlueEntry> pair in glueEntries) {
                if (pair.Value.IsTracked) {
                    pair.Value.Stick();
                }
            }

            _errCount = 0;
        }
        catch (Exception e) {
            _errCount++;
            RefreshCuas();
            if (_errCount >= 3) {
                Err($"[VamCuaSuperglue] errored ({_errCount}) times: {e}");
                Destroy(this);
            }
        }
    }

    public class GlueEntry {
        public Atom cuaAtom;
        public Transform transform;
        public Pose prevPose;
        public Pose thisPose;
        public bool isPhysicsEnabled;
        public bool isPosParentLink;
        public bool isRotParentLink;

        private JSONStorableBool _isPhysicsEnabledJson;
        private JSONStorableStringChooser _isPosParentLinkJson;
        private JSONStorableStringChooser _isRotParentLinkJson;

        public bool IsTracked => cuaAtom.mainController.linkToRB &&
                                 isPhysicsEnabled &&
                                 transform;

        public GlueEntry(Atom atom) {
            cuaAtom = atom;
            transform = atom.transform.Find("reParentObject/object");
            if (!transform) {
                if (debug) {
                    Err($"[VamCuaSuperglue] Transform is null on atom {atom.name}");
                }
                // isPhysicsEnabled = true; // Don't spam errors
            }

            JSONStorable control = atom.GetStorableByID("control");

            _isPhysicsEnabledJson = control.GetBoolJSONParam("physicsEnabled");
            _isPosParentLinkJson = control.GetStringChooserJSONParam("positionState");
            _isRotParentLinkJson = control.GetStringChooserJSONParam("rotationState");

            _isPhysicsEnabledJson.setCallbackFunction += PhysicsCallback;
            _isPosParentLinkJson.setCallbackFunction += PosCallback;
            _isRotParentLinkJson.setCallbackFunction += RotCallback;

            if (transform) {
                prevPose = new Pose(transform.position, transform.rotation);
                thisPose = prevPose;
            }
            RefreshGlue(atom);
        }

        public void RefreshGlue(Atom atom){
            PhysicsCallback(_isPhysicsEnabledJson.valNoCallback);
            PosCallback(_isPosParentLinkJson.valNoCallback);
            RotCallback(_isPosParentLinkJson.valNoCallback);

            DbgLog(atom.name + ": " + isPhysicsEnabled + " " + isPosParentLink + " " + isRotParentLink);
        }

        public void PhysicsCallback(bool val) {
            isPhysicsEnabled = val;
            singleton.UpdateInfoPanel();
        }

        public void PosCallback(string val) {
            isPosParentLink = val.Equals("ParentLink");
            singleton.UpdateInfoPanel();
        }

        public void RotCallback(string val) {
            isRotParentLink = val.Equals("ParentLink");
            singleton.UpdateInfoPanel();
        }

        public void Stick() {
            thisPose = new Pose(transform.position, transform.rotation);

            if (isPosParentLink) {
                transform.position = prevPose.position;
            }
            if (isRotParentLink) {
                transform.rotation = prevPose.rotation;
            }

            prevPose = thisPose;
        }

        public void Unsubscribe() {
            _isPhysicsEnabledJson.setCallbackFunction -= PhysicsCallback;
            _isPosParentLinkJson.setCallbackFunction -= PosCallback;
            _isRotParentLinkJson.setCallbackFunction -= RotCallback;
        }
    }


    #region Util

    public const bool debug = false;

    /// <summary>
    /// Debug log is only called with the debug flag enabled
    /// </summary>
    /// <param name="message"></param>
    public static void DbgLog(string message) {
        if (!debug) return;
        SuperController.LogMessage(message);
    }

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

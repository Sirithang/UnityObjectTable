using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Linq;

public class Plinth : EditorWindow
{
    public class TypeInfo
    {
        public System.Type type;
        public bool isScriptableObject;
    }

    protected DatabaseDisplayer _databaseDisplay;

    protected string[] _objectTypeNames;
    protected TypeInfo[] _objectTypeInfos;

    [SerializeField] protected int _selectedIndex = -1;
    [SerializeField] protected string _targetFolderForNewAsset = "";

    [MenuItem("Window/Plinth (Object Table)")]
    static void Open()
    {
        GetWindow<Plinth>();
    }

    private void OnEnable()
    {
        _databaseDisplay = null;
        RetrieveType();

        if (_selectedIndex != -1 && _selectedIndex < _objectTypeNames.Length)
            CreateDisplayFrom(_objectTypeInfos[_selectedIndex]);
    }

    private void RetrieveType()
    {
        //TODO : search for better way. Could be slow on big project + excluding assembly with Unity & Editor (to avoid grabbing internal Unity object like editor windows etc. that
        //derive from Scriptable object too) may lead to missing some package/plugin scriptable objects type
        var subclassesScriptableObject =
                        from assembly in System.AppDomain.CurrentDomain.GetAssemblies()
                        where (!assembly.FullName.Contains("Unity") && !assembly.FullName.Contains("Editor"))
                        from type in assembly.GetTypes()
                        where type.IsSubclassOf(typeof(ScriptableObject))
                        select type;

        var subclassesMonoBehaviour =
                        from assembly in System.AppDomain.CurrentDomain.GetAssemblies()
                        where (!assembly.FullName.Contains("Unity") && !assembly.FullName.Contains("Editor"))
                        from type in assembly.GetTypes()
                        where type.IsSubclassOf(typeof(MonoBehaviour))
                        select type;

        _objectTypeInfos = new TypeInfo[subclassesScriptableObject.Count() + subclassesMonoBehaviour.Count()];
        _objectTypeNames = new string[_objectTypeInfos.Length];

        int i = 0;
        foreach (var t in subclassesScriptableObject)
        {
            _objectTypeInfos[i] = new TypeInfo();
            _objectTypeInfos[i].type = t;
            _objectTypeInfos[i].isScriptableObject = true;
            _objectTypeNames[i] = t.Name;
            i++;
        }

        foreach (var t in subclassesMonoBehaviour)
        {
            _objectTypeInfos[i] = new TypeInfo();
            _objectTypeInfos[i].type = t;
            _objectTypeInfos[i].isScriptableObject = false;
            _objectTypeNames[i] = t.Name;
            i++;
        }
    }

    void CreateDisplayFrom(TypeInfo type)
    {
        TreeViewState state = new TreeViewState();

        GameObject tempGO = null;
        UnityEngine.Object temp;

        if(type.isScriptableObject)
            temp = CreateInstance(type.type);
        else
        {
            tempGO = new GameObject();
            temp = tempGO.AddComponent(type.type);
        }

        SerializedObject tempObj = new SerializedObject(temp);

        SerializedProperty prop = tempObj.GetIterator();

        //Count remainingwill count hte m_Script property (the type of script, like the 1st field in the inspector)
        //but we will replace it with the (hidden) propery that is the name, so it even out
        int propCount = prop.CountRemaining(); // but this is incorrect when there's arrays involved!

        prop.Reset();
        prop.Next(true);
        //do it once to "jump over" the script file, we don't want that property.
        prop.NextVisible(false);

        var columns = new List<MultiColumnHeaderState.Column>();

        // add name
        var objectColumn = new MultiColumnHeaderState.Column();
        objectColumn.headerContent = new GUIContent("Object");
        objectColumn.width = 64;
        columns.Add(objectColumn);

        while (prop.NextVisible(false))
        {
            // Debug.Log(prop.name);
            prop.isExpanded = false;
            var newColumn = new MultiColumnHeaderState.Column();
            newColumn.allowToggleVisibility = false;
            newColumn.headerContent = new GUIContent(prop.displayName);
            newColumn.minWidth = GetPropertyWidthFromType(prop.propertyType);
            newColumn.width = newColumn.minWidth;
            newColumn.canSort = CanSort(prop.propertyType);
            columns.Add(newColumn);
        }

        MultiColumnHeaderState headerstate = new MultiColumnHeaderState(columns.ToArray());
        MultiColumnHeader header = new MultiColumnHeader(headerstate);

        _databaseDisplay = new DatabaseDisplayer(state, header, type);
        _databaseDisplay.Reload();

        if (tempGO != null)
            DestroyImmediate(tempGO);
    }
    
    bool CanSort(SerializedPropertyType type)
    {
        switch (type)
        {
            case SerializedPropertyType.AnimationCurve:
            case SerializedPropertyType.Bounds:
            case SerializedPropertyType.BoundsInt:
            case SerializedPropertyType.Character:
            case SerializedPropertyType.Color:
            case SerializedPropertyType.ExposedReference:
            case SerializedPropertyType.FixedBufferSize:
            case SerializedPropertyType.Generic:
            case SerializedPropertyType.Gradient:
            case SerializedPropertyType.ObjectReference:
            case SerializedPropertyType.Quaternion:
            case SerializedPropertyType.Rect:
            case SerializedPropertyType.RectInt:
            case SerializedPropertyType.Vector2:
            case SerializedPropertyType.Vector2Int:
            case SerializedPropertyType.Vector3:
            case SerializedPropertyType.Vector3Int:
            case SerializedPropertyType.Vector4:
                return false;
            default:
                break;
        }

        return true;
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        EditorGUIUtility.labelWidth = 40;
        Rect controlRect = new Rect(0, 0, 200, EditorGUIUtility.singleLineHeight);
        int selected = EditorGUI.Popup(controlRect, "Type", _selectedIndex, _objectTypeNames);

        EditorGUIUtility.labelWidth = 0;

        if (EditorGUI.EndChangeCheck())
        {
            if (selected >= 0)
            {
                CreateDisplayFrom(_objectTypeInfos[selected]);
                _selectedIndex = selected;
            }
        }

        if (_databaseDisplay != null)
        {
            if (_selectedIndex != -1 && _objectTypeInfos[_selectedIndex].isScriptableObject)
            {
                // controlRect.y += controlRect.height;
                controlRect.x = 232;
                controlRect.width = 64;

                if (GUI.Button(controlRect, "New"))
                {
                    ScriptableObject newObj = CreateInstance(_objectTypeInfos[_selectedIndex].type);

                    string path = AssetDatabase.GenerateUniqueAssetPath("Assets" + _targetFolderForNewAsset + "/new" + _objectTypeNames[_selectedIndex] + ".asset");

                    AssetDatabase.CreateAsset(newObj, path);
                    AssetDatabase.Refresh();

                    newObj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                    _databaseDisplay.AddElement(newObj);
                }

                controlRect.x += controlRect.width + 12;
                controlRect.width = 100;

                if (GUI.Button(controlRect, "Pick Folder"))
                {
                    string fullCurrentPath = Application.dataPath + _targetFolderForNewAsset;

                    string value = EditorUtility.SaveFolderPanel("Pick default folder for new asset", fullCurrentPath, "database");
                    if (value.Length != 0)
                    {
                        if (!value.Contains(Application.dataPath))
                        {
                            Debug.LogErrorFormat("Picked folder {0} isn't in the Assets folder. Pick a folder in the Assets folder", value);
                        }
                        else
                            _targetFolderForNewAsset = value.Replace(Application.dataPath, "");
                    }
                }

                controlRect.x += controlRect.width;
                controlRect.width = 500;

                EditorGUI.SelectableLabel(controlRect, _targetFolderForNewAsset == "" ? "None, Pick a folder where new assets will be created" : _targetFolderForNewAsset);
            }

            float startY = controlRect.y + controlRect.height + 12;
            Rect r = new Rect(0, startY, position.width, position.height - startY);

            _databaseDisplay.OnGUI(r);
        }
    }

    //TODO : find if there is an automated way to get "optimal" size for a SerializedProperty
    private float GetPropertyWidthFromType(SerializedPropertyType type)
    {
        float newSize = 32;

        switch (type)
        {
            case SerializedPropertyType.AnimationCurve:
                newSize = 128;
                break;
            case SerializedPropertyType.Vector2:
            case SerializedPropertyType.Vector2Int:
                newSize = 64 * 2;
                break;
            case SerializedPropertyType.Vector3:
            case SerializedPropertyType.Vector3Int:
                newSize = 64 * 3;
                break;
            case SerializedPropertyType.Vector4:
                newSize = 64 * 4;
                break;
            case SerializedPropertyType.Float:
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.String:
            case SerializedPropertyType.Boolean:
                newSize = 64;
                break;
            case SerializedPropertyType.ManagedReference:
            case SerializedPropertyType.ObjectReference:
            case SerializedPropertyType.Generic:
                newSize = 128;
                break;
            default:
                break;
        }

        return newSize;
    }
}

public class DatabaseDisplayer : TreeView
{
    protected int _freeID = 0;
    protected Plinth.TypeInfo _objectType;

    public DatabaseDisplayer(TreeViewState state, MultiColumnHeader header, Plinth.TypeInfo objectType) : base(state, header)
    {
        _freeID = 0;
        _objectType = objectType;

        showAlternatingRowBackgrounds = true;
        showBorder = true;
        cellMargin = 6;

        multiColumnHeader.sortingChanged += OnSortingChanged;
        multiColumnHeader.ResizeToFit();
    }

    void OnSortingChanged(MultiColumnHeader multiColumnHeader)
    {
        Sort(GetRows());
        Repaint();
    }

    public int GetNewID()
    {
        int id = _freeID;
        _freeID += 1;

        return id;
    }

    public void AddElement(ScriptableObject newObject)
    {
        var rows = GetRows();
        var newItem = DatabaseViewerItem.CreateFromUnityObject(newObject, this);

        rootItem.AddChild(newItem);
        rows.Add(newItem);

        Sort(rows);

        Repaint();
    }

    protected override void KeyEvent()
    {
        if(Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Delete)
        {
            var list = GetSelection();
            if (EditorUtility.DisplayDialog("Confirm", "Confirm the suppression of the " + list.Count + " elected element?\nThis can't be undone.", "Yes", "No"))
            {
                var rows = GetRows();
                foreach (var idx in list)
                {
                    DatabaseViewerItem itm = FindItem(idx, rootItem) as DatabaseViewerItem;
                    rows.Remove(itm);

                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(itm.obj.targetObject));
                }

                AssetDatabase.Refresh();
                Repaint();
            }

        }
        else
            base.KeyEvent();
    }

    void Sort(IList<TreeViewItem> rows)
    {
        if (multiColumnHeader.sortedColumnIndex == -1)
            return;

        if (rows.Count == 0)
            return;

        int sortedColumn = multiColumnHeader.sortedColumnIndex;
        var childrens = rootItem.children.Cast<DatabaseViewerItem>();

        var comparer = new SerializePropertyComparer();
        var ordered = multiColumnHeader.IsSortedAscending(sortedColumn) ? childrens.OrderBy(k => k.properties[sortedColumn], comparer) : childrens.OrderByDescending(k => k.properties[sortedColumn], comparer);

        rows.Clear();
        foreach (var v in ordered)
            rows.Add(v);
    }

    protected override void RowGUI(RowGUIArgs args)
    {
        var item = (DatabaseViewerItem)args.item;
        item.obj.Update();

        for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
        {
            Rect r = args.GetCellRect(i);
            r.height = EditorGUIUtility.singleLineHeight;
            int column = args.GetColumn(i);
            int idx = column;

            if (idx == 0)
            {
                // add mini object field to inspect easily?
                const int OBJECT_WIDTH = 32;
                var fullWidth = r.width;
                r.width = OBJECT_WIDTH;
                GUI.enabled = false;
                EditorGUI.ObjectField(r, item.obj.targetObject, _objectType.type, false);

                GUI.enabled = true;
                r.width = fullWidth - OBJECT_WIDTH;
                r.x = OBJECT_WIDTH;

                //we handle the name a bit differently, as any change in the name need to be reflected in the name of the asset. So rename the asset if the name is changed
                string originalValue;
                if (_objectType.isScriptableObject)
                    originalValue = item.properties[idx].stringValue;
                else
                    originalValue = item.obj.targetObject.name;

                string name = EditorGUI.DelayedTextField(r, originalValue);

                if (name != originalValue)
                {
                    string oldPath = AssetDatabase.GetAssetPath(item.obj.targetObject);

                    string error = AssetDatabase.RenameAsset(oldPath, System.IO.Path.GetFileNameWithoutExtension(name));
                    if (error != "")
                        Debug.LogError(error);
                }
            }
            else
            {
                bool isExpand = item.properties[idx].isExpanded;
                bool wantsExpand = EditorGUI.PropertyField(r, item.properties[idx], GUIContent.none, true);
                if (isExpand != wantsExpand) {
                    RefreshCustomRowHeights();
                }
            }
        }

        item.obj.ApplyModifiedProperties();
    }

    protected override float GetCustomRowHeight(int row, TreeViewItem treeItem) {
        float height = 0;
        var item = (DatabaseViewerItem)treeItem;
        for(int i=0; i<item.properties.Length; i++) {
            float newHeight = EditorGUI.GetPropertyHeight(item.properties[i], item.properties[i].isArray && item.properties[i].isExpanded);
            height = Mathf.Max(height, newHeight);
        }
        return height;
    }

    protected override TreeViewItem BuildRoot()
    {
        Object[] objs = null;

        //scriptable object type can be find fast through the find method, but monobheaviour need to be queried on EVERY PREFABS IN THE PROJECTS
        //TODO : find a better way, this will probably become VERY SLOW on big project with thousand of prefabs
        if(_objectType.isScriptableObject)
        {
            string[] assets = AssetDatabase.FindAssets("t:"+_objectType.type.ToString());
            objs = new Object[assets.Length];

            for (int i = 0; i < assets.Length; ++i)
            {
                objs[i] = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assets[i]), _objectType.type);
            }
        }
        else
        {
            objs = new Object[0];
            string[] assets = AssetDatabase.FindAssets("t:Prefab");

            for (int i = 0; i < assets.Length; ++i)
            {
                Object obj  = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assets[i]), _objectType.type);
                if (obj != null)
                    ArrayUtility.Add(ref objs, obj);
            }
        }

        TreeViewItem root = new TreeViewItem();

        root.depth = -1;
        root.id = -1;
        root.parent = null;
        root.children = new List<TreeViewItem>();

        if (objs != null)
        {
            for (int i = 0; i < objs.Length; ++i)
            {
                var child = DatabaseViewerItem.CreateFromUnityObject(objs[i], this);

                root.AddChild(child);
            }
        }

        return root;
    }


}

public class DatabaseViewerItem : TreeViewItem
{
    //TODO : not too happy with stocking reference to so many thing, prob waste tons of space
    //but as we can't access property by index, need to build an array from them
    public SerializedObject obj;
    public SerializedProperty[] properties;

    public static DatabaseViewerItem CreateFromUnityObject(UnityEngine.Object unityObject, DatabaseDisplayer treeView)
    {
        SerializedObject so = new SerializedObject(unityObject);

        DatabaseViewerItem newItem = new DatabaseViewerItem();
        newItem.children = new List<TreeViewItem>();
        newItem.depth = 0;
        newItem.id = treeView.GetNewID();
        newItem.obj = so;

        SerializedProperty prop = so.GetIterator();
        prop.Next(true);
        prop.NextVisible(false);

        newItem.properties = new SerializedProperty[treeView.multiColumnHeader.state.columns.Length];
        newItem.properties[0] = so.FindProperty("m_Name");
        for (int k = 1; k < newItem.properties.Length; ++k)
        {
            prop.NextVisible(false);
            newItem.properties[k] = prop.Copy();
        }

        return newItem;
    }
}

public class SerializePropertyComparer : IComparer<SerializedProperty>
{
    public int Compare(SerializedProperty x, SerializedProperty y)
    {
        return GenericCompare(x, y);
    }

    // TODO : look for a betetr way this probably generate ton of garbage + need to be extended manually
    int GenericCompare(SerializedProperty a, SerializedProperty b)
    {
        if (a.propertyType != b.propertyType)
        {
            Debug.LogError("Couldn't compare 2 SerializedProeprty of different type");
            return 0;
        }

        switch (a.propertyType)
        {
            case SerializedPropertyType.AnimationCurve:
            case SerializedPropertyType.Bounds:
            case SerializedPropertyType.BoundsInt:
            case SerializedPropertyType.Character:
            case SerializedPropertyType.Color:
            case SerializedPropertyType.ExposedReference:
            case SerializedPropertyType.FixedBufferSize:
            case SerializedPropertyType.Generic:
            case SerializedPropertyType.Gradient:
            case SerializedPropertyType.ObjectReference:
            case SerializedPropertyType.Quaternion:
            case SerializedPropertyType.Rect:
            case SerializedPropertyType.RectInt:
            case SerializedPropertyType.Vector2:
            case SerializedPropertyType.Vector2Int:
            case SerializedPropertyType.Vector3:
            case SerializedPropertyType.Vector3Int:
            case SerializedPropertyType.Vector4:
                return 0;
            case SerializedPropertyType.Boolean:
                return a.boolValue.CompareTo(b.boolValue);
            case SerializedPropertyType.Enum:
                return a.enumValueIndex.CompareTo(b.enumValueIndex);
            case SerializedPropertyType.Float:
                return a.floatValue.CompareTo(b.floatValue);
            case SerializedPropertyType.Integer:
                return a.intValue.CompareTo(b.intValue);
            case SerializedPropertyType.LayerMask: //really sueful to comapre layer mask int value?? 
                return a.intValue.CompareTo(b.intValue);
            case SerializedPropertyType.String:
                return a.stringValue.CompareTo(b.stringValue);
        }

        return 0;
    }
}
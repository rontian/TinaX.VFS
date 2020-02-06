﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.EditorTools;
using TinaX;
using TinaX.VFSKit;
using System.Linq;

namespace TinaXEditor.VFSKit
{
    public class VFSConfigDashboardIMGUI : EditorWindow
    {
        [MenuItem("TinaX/VFS/VFS Dashboard")]
        public static void OpenUI()
        {
            VFSConfigDashboardIMGUI wnd = GetWindow<VFSConfigDashboardIMGUI>();
            wnd.titleContent = new GUIContent(VFSConfigDashboardI18N.WindowTitle);
        }

        private int Window_Min_Weight = Window_Area_GlobalConfig_Min_Weight + Window_Area_GroupList_Min_Weight + Window_Area_GroupConfig_Min_Weight;
        private const int Window_Area_GlobalConfig_Min_Weight = 300;
        private const int Window_Area_GroupList_Min_Weight = 280;
        private const int Window_Area_GroupConfig_Min_Weight = 400;



        private const string ConfigFileName = "VFSConfig";
        private VFSConfigModel mVFSConfig;
        private SerializedObject mVFSConfigSerializedObject;

        /// <summary>
        /// 相对Resources的路径
        /// </summary>
        private string mConfigFilePath = $"{TinaX.Const.FrameworkConst.Framework_Configs_Folder_Path}/{ConfigFileName}";

        private GUIStyle style_title_h2;
        private GUIStyle style_title_h2_center;
        private GUIStyle style_title_h3;


        bool b_flodout_global_extname = false;
        private ReorderableList reorderableList_global_extname;

        private Vector2 v2_scrollview_assetGroup = Vector2.zero;
        private string input_createGroupName;
        private ReorderableList reorderableList_groups;

        private int? cur_select_group_index;
        private int cur_group_drawing_data_index = -1;
        private ReorderableList reorderableList_groups_folderList;
        private ReorderableList reorderableList_groups_assetList;
        private Vector2 v2_scrollview_assetGroupConfig = Vector2.zero;

        /// <summary>
        /// “文件夹” 图标
        /// </summary>
        private Texture img_folder_icon;
        private Texture img_file_icon;

        private void OnEnable()
        {
            //try to get vfs config
            mVFSConfig = XConfig.GetConfig<VFSConfigModel>(mConfigFilePath);

            this.minSize = new Vector2(Window_Min_Weight, 600);
            


            style_title_h2 = new GUIStyle(EditorStyles.label);
            style_title_h2.fontStyle = FontStyle.Bold;
            style_title_h2.fontSize = 22;
            
            style_title_h2_center = new GUIStyle(EditorStyles.label);
            style_title_h2_center.fontStyle = FontStyle.Bold;
            style_title_h2_center.fontSize = 22;
            style_title_h2_center.alignment = TextAnchor.MiddleCenter;

            style_title_h3 = new GUIStyle(EditorStyles.label);
            style_title_h3.fontStyle = FontStyle.Bold;
            style_title_h3.fontSize = 18;


            img_folder_icon = AssetDatabase.LoadAssetAtPath<Texture>("Packages/io.nekonya.tinax.vfs/Editor/Res/Icons/folder.png");
            img_file_icon = AssetDatabase.LoadAssetAtPath<Texture>("Packages/io.nekonya.tinax.vfs/Editor/Res/Icons/file.png");
        }

        private void OnGUI()
        {
            if (mVFSConfig == null)
            {
                EditorGUILayout.BeginVertical();
                GUILayout.Space(35);
                GUILayout.Label("TinaX Virtual File System (VFS) config file not ready. Click \"Create\"button to start use VFS.");
                if (GUILayout.Button("Create"))
                {
                    mVFSConfig = XConfig.CreateConfigIfNotExists<VFSConfigModel>(mConfigFilePath);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }
            else
            {
                if (mVFSConfigSerializedObject == null)
                {
                    mVFSConfigSerializedObject = new SerializedObject(mVFSConfig);
                }


                //绘制顶部工具栏
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label("VFS Editor");

                //工具栏 -> 配置
                //EditorGUILayout.BeginHorizontal(EditorStyles.toolbarPopup);
                //GUILayout.Button("喵");
                //GUILayout.Button("喵2");
                //EditorGUILayout.EndHorizontal();
                //工具栏 -> Build
                GUILayout.FlexibleSpace();
                GUILayout.Button("Build", EditorStyles.toolbarButton,GUILayout.MinWidth(75),GUILayout.MaxWidth(76));

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal(GUILayout.MinWidth(Window_Min_Weight));
                //左边：全局设置
                DrawGlobalConfig();

                //中间： 资源组设置
                DrawAssetsGroupConfig();

                EditorGUILayout.Space(1,true);
                //右侧 资源组细节
                DrawGroupConfig();

                EditorGUILayout.EndHorizontal();


            }
        }

        private void OnDestroy()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            VFSManagerEditor.RefreshManager(true);
        }

        private void DrawGlobalConfig()
        {
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(450), GUILayout.MinWidth(Window_Area_GlobalConfig_Min_Weight));
            GUILayout.Label("Global", style_title_h2);
            EditorGUILayout.Space();

            //忽略后缀名
            b_flodout_global_extname = EditorGUILayout.Foldout(b_flodout_global_extname, VFSConfigDashboardI18N.GlobalVFS_Ignore_ExtName);
            if (b_flodout_global_extname)
            {
                if (reorderableList_global_extname == null)
                {
                    reorderableList_global_extname = new ReorderableList(mVFSConfigSerializedObject,
                                                                         mVFSConfigSerializedObject.FindProperty("GlobalVFS_Ignore_ExtName"),
                                                                         true,
                                                                         true,
                                                                         true,
                                                                         true);
                    reorderableList_global_extname.drawElementCallback = (rect, index, selected, focused) =>
                    {
                        SerializedProperty itemData = reorderableList_global_extname.serializedProperty.GetArrayElementAtIndex(index);
                        rect.y += 2;
                        rect.height = EditorGUIUtility.singleLineHeight;
                        EditorGUI.PropertyField(rect, itemData, GUIContent.none);
                    };
                    reorderableList_global_extname.drawHeaderCallback = (rect) =>
                    {
                        GUI.Label(rect, VFSConfigDashboardI18N.GlobalVFS_Ignore_ExtName);
                    };
                    reorderableList_global_extname.onAddCallback = (list) =>
                    {
                        if (list.serializedProperty != null)
                        {
                            list.serializedProperty.arraySize++;
                            list.index = list.serializedProperty.arraySize - 1;

                            SerializedProperty itemData = list.serializedProperty.GetArrayElementAtIndex(list.index);
                            itemData.stringValue = string.Empty;
                        }
                        else
                        {
                            ReorderableList.defaultBehaviours.DoAddButton(list);
                        }
                    };
                }

                reorderableList_global_extname.DoLayoutList();
                mVFSConfigSerializedObject.ApplyModifiedProperties();

            }

            ////Groups
            //EditorGUILayout.Space();
            //GUILayout.Label("Groups", EditorStyles.boldLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawAssetsGroupConfig()
        {
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(Window_Area_GroupList_Min_Weight));
            //小toolbar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            //num
            GUILayout.Label("Current Groups Num: ");
            EditorGUILayout.Space();
            input_createGroupName = EditorGUILayout.TextField(input_createGroupName, EditorStyles.toolbarTextField);
            if (GUILayout.Button("Create Group"))
            {
                if (input_createGroupName.IsNullOrEmpty())
                {
                    EditorUtility.DisplayDialog(VFSConfigDashboardI18N.MsgBox_Common_Error, VFSConfigDashboardI18N.MsgBox_Msg_CreateGroupNameIsNull, VFSConfigDashboardI18N.MsgBox_Common_Confirm);
                    return;
                }
                //检查重复
                if (mVFSConfig.Groups.Any(g => g.GroupName == input_createGroupName))
                {
                    EditorUtility.DisplayDialog(VFSConfigDashboardI18N.MsgBox_Common_Error, string.Format(VFSConfigDashboardI18N.MsgBox_Msg_CreateGroupNameHasExists, input_createGroupName), VFSConfigDashboardI18N.MsgBox_Common_Confirm);
                    return;
                }

                //create
                if (mVFSConfig.Groups == null) mVFSConfig.Groups = new VFSGroupOption[] { };
                List<VFSGroupOption> gs_temp = new List<VFSGroupOption>(mVFSConfig.Groups);
                gs_temp.Add(new VFSGroupOption() { GroupName = input_createGroupName });
                mVFSConfig.Groups = gs_temp.ToArray();

                mVFSConfigSerializedObject.UpdateIfRequiredOrScript();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

            }
            EditorGUILayout.EndHorizontal();



            v2_scrollview_assetGroup = EditorGUILayout.BeginScrollView(v2_scrollview_assetGroup);

            if (mVFSConfig.Groups.Length <= 0)
            {
                GUILayout.Label(VFSConfigDashboardI18N.Groups_Item_Null_Tips);
            }
            else
            {
                if (reorderableList_groups == null)
                {
                    reorderableList_groups = new ReorderableList(mVFSConfigSerializedObject,
                                                                         mVFSConfigSerializedObject.FindProperty("Groups"),
                                                                         true,
                                                                         true,
                                                                         true,
                                                                         true);

                    reorderableList_groups.drawElementCallback = (rect, index, selected, focused) =>
                    {
                        SerializedProperty itemData = reorderableList_groups.serializedProperty.GetArrayElementAtIndex(index);
                        var cur_data = mVFSConfig.Groups[index];
                        rect.y += 2;
                        //name
                        GUI.Label(rect, cur_data.GroupName, EditorStyles.boldLabel);
                        //rect.height = EditorGUIUtility.singleLineHeight;
                        //EditorGUI.PropertyField(rect, itemData, GUIContent.none);

                        if (selected)
                        {
                            cur_select_group_index = index;
                        }
                        else if (cur_select_group_index == index)
                        {
                            cur_select_group_index = null;
                        }
                    };
                    reorderableList_groups.drawHeaderCallback = (rect) =>
                    {
                        GUI.Label(rect, "Groups");
                    };
                }
                reorderableList_groups.DoLayoutList();

            }


            GUILayout.FlexibleSpace();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawGroupConfig()
        {
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(Window_Area_GroupConfig_Min_Weight));

            if(mVFSConfig.Groups == null || mVFSConfig.Groups.Length <= 0)
            {
                cur_select_group_index = null;
            }

            if (cur_select_group_index == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(VFSConfigDashboardI18N.Window_GroupConfig_Null_Tips, style_title_h2_center);
                GUILayout.FlexibleSpace();

                cur_group_drawing_data_index = -1;
            }
            else
            {
                v2_scrollview_assetGroupConfig = GUILayout.BeginScrollView(v2_scrollview_assetGroupConfig);
                //Group Name
                GUILayout.Label(mVFSConfig.Groups[cur_select_group_index.Value].GroupName, style_title_h3);

                GUILayout.BeginHorizontal();
                GUILayout.Label(VFSConfigDashboardI18N.Window_GroupConfig_Title_GroupName,GUILayout.MaxWidth(90));
                mVFSConfig.Groups[cur_select_group_index.Value].GroupName = GUILayout.TextField(mVFSConfig.Groups[cur_select_group_index.Value].GroupName);
                GUILayout.EndHorizontal();

                #region 文件夹列表
                //folder list 
                if (reorderableList_groups_folderList == null || (cur_select_group_index.Value != cur_group_drawing_data_index))
                {
                    reorderableList_groups_folderList = new ReorderableList(mVFSConfigSerializedObject,
                                                                            mVFSConfigSerializedObject.FindProperty("Groups").GetArrayElementAtIndex(cur_select_group_index.Value).FindPropertyRelative("FolderPaths"),
                                                                            true,
                                                                            true,
                                                                            true,
                                                                            true);
                    reorderableList_groups_folderList.drawElementCallback = (rect, index, selected, focused) =>
                    {
                        SerializedProperty itemData = reorderableList_groups_folderList.serializedProperty.GetArrayElementAtIndex(index);
                        //var cur_data = mVFSConfig.Groups[cur_select_group_index.Value].FolderPaths[index];

                        rect.y += 2;
                        rect.height = EditorGUIUtility.singleLineHeight;
                        var textArea_rect = rect;
                        textArea_rect.width -= 35;

                        EditorGUI.PropertyField(textArea_rect, itemData, GUIContent.none);

                        var btn_rect = rect;
                        btn_rect.y -= 0.5f;
                        btn_rect.x += textArea_rect.width + 2;
                        btn_rect.width = 35;
                        if(GUI.Button(btn_rect, img_folder_icon))
                        {
                            

                            var select_path = EditorUtility.OpenFolderPanel(VFSConfigDashboardI18N.Window_GroupConfig_SelectFolder, "Assets/", "");
                            if (select_path.IsNullOrEmpty()) return;
                            int asset_start_index = select_path.IndexOf("Assets/");
                            if(asset_start_index > -1)
                            {
                                select_path = select_path.Substring(asset_start_index, select_path.Length - asset_start_index);
                            }
                            
                            itemData.stringValue = select_path;
                        }
                    };
                    reorderableList_groups_folderList.drawHeaderCallback = (rect) =>
                    {
                        GUI.Label(rect, VFSConfigDashboardI18N.Window_GroupConfig_Title_FolderPaths);
                    };
                    reorderableList_groups_folderList.onAddCallback = (list) =>
                    {
                        // 调整新增item的默认值
                        if (list.serializedProperty != null)
                        {
                            list.serializedProperty.arraySize++;
                            list.index = list.serializedProperty.arraySize - 1;

                            SerializedProperty itemData = list.serializedProperty.GetArrayElementAtIndex(list.index);
                            itemData.stringValue = string.Empty;
                        }
                        else
                        {
                            ReorderableList.defaultBehaviours.DoAddButton(list);
                        }
                    };

                }
                reorderableList_groups_folderList?.DoLayoutList();

                #endregion

                #region 资源列表
                if (reorderableList_groups_assetList == null || (cur_select_group_index.Value != cur_group_drawing_data_index))
                {
                    reorderableList_groups_assetList = new ReorderableList(mVFSConfigSerializedObject,
                                                                            mVFSConfigSerializedObject.FindProperty("Groups").GetArrayElementAtIndex(cur_select_group_index.Value).FindPropertyRelative("AssetPaths"),
                                                                            true,
                                                                            true,
                                                                            true,
                                                                            true);
                    reorderableList_groups_assetList.drawElementCallback = (rect, index, selected, focused) =>
                    {
                        SerializedProperty itemData = reorderableList_groups_assetList.serializedProperty.GetArrayElementAtIndex(index);
                        //var cur_data = mVFSConfig.Groups[cur_select_group_index.Value].AssetPaths[index];

                        rect.y += 2;
                        rect.height = EditorGUIUtility.singleLineHeight;
                        var textArea_rect = rect;
                        textArea_rect.width -= 35;

                        EditorGUI.PropertyField(textArea_rect, itemData, GUIContent.none);

                        var btn_rect = rect;
                        btn_rect.y -= 0.5f;
                        btn_rect.x += textArea_rect.width + 2;
                        btn_rect.width = 35;
                        if (GUI.Button(btn_rect, img_file_icon))
                        {


                            var select_path = EditorUtility.OpenFilePanel(VFSConfigDashboardI18N.Window_GroupConfig_SelectAsset, "Assets/", "");
                            if (select_path.IsNullOrEmpty()) return;
                            int asset_start_index = select_path.IndexOf("Assets/");
                            if (asset_start_index > -1)
                            {
                                select_path = select_path.Substring(asset_start_index, select_path.Length - asset_start_index);
                            }

                            if (select_path.ToLower().EndsWith(".meta"))
                            {
                                EditorUtility.DisplayDialog(VFSConfigDashboardI18N.MsgBox_Common_Error,
                                                            VFSConfigDashboardI18N.Window_GroupConfig_SelectAsset_Error_Select_Meta,
                                                            VFSConfigDashboardI18N.MsgBox_Common_Confirm) ;
                                return;
                            }

                            itemData.stringValue = select_path;
                        }
                    };
                    reorderableList_groups_assetList.drawHeaderCallback = (rect) =>
                    {
                        GUI.Label(rect, VFSConfigDashboardI18N.Window_GroupConfig_Title_AssetPaths);
                    };
                    reorderableList_groups_assetList.onAddCallback = (list) =>
                    {
                        // 调整新增item的默认值
                        if (list.serializedProperty != null)
                        {
                            list.serializedProperty.arraySize++;
                            list.index = list.serializedProperty.arraySize - 1;

                            SerializedProperty itemData = list.serializedProperty.GetArrayElementAtIndex(list.index);
                            itemData.stringValue = string.Empty;
                        }
                        else
                        {
                            ReorderableList.defaultBehaviours.DoAddButton(list);
                        }
                    };

                }
                reorderableList_groups_assetList?.DoLayoutList();
                #endregion

                cur_group_drawing_data_index = cur_select_group_index.Value;
                mVFSConfigSerializedObject.ApplyModifiedProperties();
                GUILayout.EndScrollView();
            }
            //Group
            //GUILayout.Label("Group: " + )


            EditorGUILayout.EndVertical();
        }

    }



}


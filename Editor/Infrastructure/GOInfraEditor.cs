using Assets._Project.Scripts.UtilScripts;
using System.Collections.Generic;
using System.Linq;
using Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime.InfraScripts;
using UnityEditor;
using UnityEngine;

namespace Assets._Project.Scripts.Infrastructure
{
    [CustomEditor(typeof(GOInfra))]
    [CanEditMultipleObjects]
    public class GOInfraEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            bool didChange = false;


            static bool CheckInlinedDescription(GOInfra infra, InlinedObjectDescription description)
            {
                if (description._collectSelfAndChildGameObjectsAndComponents)
                {
                    description._collectSelfAndChildGameObjectsAndComponents = false;

                    PopulateInlinedDescription(infra, description);

                    Debug.Log("Successfully collected members.");

                    return true;
                }
                else if (description.memberExclusionSettings?.memberToExclude != null)
                {
                    ApplyMemberExclusion(infra, description);

                    return true;
                }
                else return false;
            }


            foreach (var obj in targets)
            {
                GOInfra infra = obj as GOInfra;
                if (infra.HasInlinedPrefabParts)
                {
                    var description = infra.InlinedPrefabDescription;

                    didChange = CheckInlinedDescription(infra,description);
                    if (didChange) SetDirty(infra);
                }

                if (infra.HasInlinedSceneParts)
                {
                    var description = infra.InlinedScenePlacedDescription;

                    didChange = CheckInlinedDescription(infra, description);
                    if (didChange) SetDirty(infra);
                }
            }




            if (GUILayout.Button("Refresh Asset References"))
            {
                foreach (var obj in targets)
                {
                    GOInfra infra = obj as GOInfra;
                    infra.RefreshReferencedAssets();

                    SetDirty(infra);
                }
            }

            //else if (GUILayout.Button("Add infra to all child"))
            //{
            //    infra.AddInfraToAllChildren();
            //    didChange = true;
            //}

            else if (GUILayout.Button("RemoveInfraFromAllChildren"))
            {
                foreach (var obj in targets)
                {
                    GOInfra infra = obj as GOInfra;
                    infra.RemoveInfraFromAllChildren();
                    SetDirty(infra);
                }
            }

            //note: feature is not fully developed
            //else if (GUILayout.Button("Cache components in children and self"))
            //{
            //    myTarget.CacheComponentsInChildrenAndSelf();
            //}


            void SetDirty(Object obj)
            {
                EditorUtility.SetDirty(obj);
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
            }
        }



        public static void PopulateInlinedDescription(GOInfra infra, InlinedObjectDescription description)
        {
            HashSet<Object> ignoredMembers = description.memberExclusionSettings.excludedMembers.ToHashSet();


            description.members.Clear();

            var components = new List<Component>();

            void Traverse(Transform parent)
            {
                void AddMember(Object member)
                {
                    if (ignoredMembers.Contains(member))
                    {
                        return;
                    }

                    var objectMember = new InlinedObjectDescription.ObjectMember()
                    {
                        member = member,
                        memberId = RandomId.New,
                    };

                    description.members.Add(objectMember);
                }

                AddMember(parent.gameObject);

                components.Clear();
                parent.GetComponents<Component>(components);

                foreach (var component in components) AddMember(component);

                for (int i = 0; i < parent.childCount; i++) Traverse(parent.GetChild(i));
            }

            Traverse(infra.transform);


        }




        public static void ApplyMemberExclusion(GOInfra infra, InlinedObjectDescription description)
        {

            HashSet<Object> alreadyExcludedMembers = description.memberExclusionSettings.excludedMembers.ToHashSet();


            var memberToExclude = description.memberExclusionSettings.memberToExclude;

            List<Object> relatedMembers = new()
                    {
                        memberToExclude
                    };

            if (memberToExclude is GameObject go)
            {
                var comps = go.GetComponents<Component>();
                relatedMembers.AddRange(comps);
            }
            else if (memberToExclude is ParticleSystem ps)
            {
                relatedMembers.Add(ps.gameObject.GetComponent<ParticleSystemRenderer>());
            }


            relatedMembers = relatedMembers.Except(alreadyExcludedMembers).ToList();


            description.memberExclusionSettings.excludedMembers.AddRange(relatedMembers);

            PopulateInlinedDescription(infra, description);

            Debug.Log($"Updated member list to exclude members:\n" +
                $"{string.Join("\n", relatedMembers.Select(m => m == null ? "null" : m.name))}");


            description.memberExclusionSettings.memberToExclude = null;
        }
    }
}

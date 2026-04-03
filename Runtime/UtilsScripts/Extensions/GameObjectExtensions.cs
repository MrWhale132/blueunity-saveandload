
using Assets._Project.Scripts.Infrastructure;
using System;
using UnityEditor;
using UnityEngine;

namespace Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime.UtilsScripts.Extensions
{
    public static class GameObjectExtensions
    {
        public static T GetComponentInParentExcludeSelf<T>(this Component component, bool includeInactive) where T : UnityEngine.Component
        {
            return component.gameObject.GetComponentInParentExcludeSelf<T>(includeInactive);
        }

        public static T GetComponentInParentExcludeSelf<T>(this GameObject gameObject, bool includeInactive) where T : UnityEngine.Component
        {
            var parent = gameObject.transform.parent;

            while (parent != null)
            {
                bool flag = includeInactive || parent.gameObject.activeInHierarchy;

                if (flag && parent.TryGetComponent<T>(out var comp))
                    return comp;

                parent = parent.parent;
            }
            return null;
        }

        public static T GetComponentInChildrenExcludeSelf<T>(this GameObject gameObject, bool includeInactive = false) where T : UnityEngine.Component
        {
            foreach (Transform child in gameObject.transform)
            {
                bool flag = includeInactive || child.gameObject.activeInHierarchy;

                if (flag && child.TryGetComponent<T>(out var comp))
                    return comp;

                var result = child.gameObject.GetComponentInChildrenExcludeSelf<T>(includeInactive);
                if (result != null)
                    return result;
            }
            return null;
        }

        public static T[] GetComponentsInChildrenExcludeSelf<T>(this GameObject gameObject, bool includeInactive) where T : UnityEngine.Component
        {
            T[] comps = gameObject.GetComponentsInChildren<T>(includeInactive: includeInactive);

            if (comps.Length == 0)
                return comps;

            int startIndex = 0;
            if (gameObject.TryGetComponent<T>(out var selfComp))
            {
                startIndex = 1;
            }

            T[] result = new T[comps.Length - startIndex];

            Array.Copy(comps, startIndex, result, 0, result.Length);

            return result;
        }
    }
}

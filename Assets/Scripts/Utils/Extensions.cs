using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatTheFunan.Utils
{
    /// <summary>
    /// Extension methods for common Unity and C# types.
    /// </summary>
    public static class Extensions
    {
        #region Transform Extensions
        /// <summary>
        /// Reset the transform's local position, rotation, and scale.
        /// </summary>
        public static void ResetLocal(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Set the X component of the position.
        /// </summary>
        public static void SetPositionX(this Transform transform, float x)
        {
            Vector3 pos = transform.position;
            pos.x = x;
            transform.position = pos;
        }

        /// <summary>
        /// Set the Y component of the position.
        /// </summary>
        public static void SetPositionY(this Transform transform, float y)
        {
            Vector3 pos = transform.position;
            pos.y = y;
            transform.position = pos;
        }

        /// <summary>
        /// Set the Z component of the position.
        /// </summary>
        public static void SetPositionZ(this Transform transform, float z)
        {
            Vector3 pos = transform.position;
            pos.z = z;
            transform.position = pos;
        }

        /// <summary>
        /// Destroy all children of the transform.
        /// </summary>
        public static void DestroyAllChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Get all children of the transform.
        /// </summary>
        public static List<Transform> GetAllChildren(this Transform transform)
        {
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                children.Add(transform.GetChild(i));
            }
            return children;
        }

        /// <summary>
        /// Look at target on the Y axis only (for 2.5D or top-down games).
        /// </summary>
        public static void LookAtY(this Transform transform, Vector3 target)
        {
            Vector3 direction = target - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        #endregion

        #region Vector Extensions
        /// <summary>
        /// Get a new Vector3 with modified X.
        /// </summary>
        public static Vector3 WithX(this Vector3 v, float x)
        {
            return new Vector3(x, v.y, v.z);
        }

        /// <summary>
        /// Get a new Vector3 with modified Y.
        /// </summary>
        public static Vector3 WithY(this Vector3 v, float y)
        {
            return new Vector3(v.x, y, v.z);
        }

        /// <summary>
        /// Get a new Vector3 with modified Z.
        /// </summary>
        public static Vector3 WithZ(this Vector3 v, float z)
        {
            return new Vector3(v.x, v.y, z);
        }

        /// <summary>
        /// Convert Vector3 to Vector2 (XY).
        /// </summary>
        public static Vector2 ToVector2XY(this Vector3 v)
        {
            return new Vector2(v.x, v.y);
        }

        /// <summary>
        /// Convert Vector3 to Vector2 (XZ).
        /// </summary>
        public static Vector2 ToVector2XZ(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        /// <summary>
        /// Get the flat distance (ignoring Y).
        /// </summary>
        public static float FlatDistance(this Vector3 a, Vector3 b)
        {
            return Vector2.Distance(a.ToVector2XZ(), b.ToVector2XZ());
        }

        /// <summary>
        /// Get a random point within a radius.
        /// </summary>
        public static Vector3 RandomPointInRadius(this Vector3 center, float radius)
        {
            Vector2 random2D = UnityEngine.Random.insideUnitCircle * radius;
            return center + new Vector3(random2D.x, 0, random2D.y);
        }
        #endregion

        #region Color Extensions
        /// <summary>
        /// Get a new Color with modified alpha.
        /// </summary>
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// Convert Color to hex string.
        /// </summary>
        public static string ToHex(this Color color)
        {
            return ColorUtility.ToHtmlStringRGBA(color);
        }
        #endregion

        #region GameObject Extensions
        /// <summary>
        /// Get or add a component.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Check if the GameObject has a component.
        /// </summary>
        public static bool HasComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() != null;
        }

        /// <summary>
        /// Set the layer of the GameObject and all children.
        /// </summary>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }
        #endregion

        #region Collection Extensions
        /// <summary>
        /// Get a random element from a list.
        /// </summary>
        public static T Random<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
            {
                return default;
            }
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Get a random element from an array.
        /// </summary>
        public static T Random<T>(this T[] array)
        {
            if (array == null || array.Length == 0)
            {
                return default;
            }
            return array[UnityEngine.Random.Range(0, array.Length)];
        }

        /// <summary>
        /// Shuffle a list in place.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// Check if the index is valid for the list.
        /// </summary>
        public static bool IsValidIndex<T>(this IList<T> list, int index)
        {
            return index >= 0 && index < list.Count;
        }

        /// <summary>
        /// Get an element or default if index is invalid.
        /// </summary>
        public static T GetOrDefault<T>(this IList<T> list, int index, T defaultValue = default)
        {
            return list.IsValidIndex(index) ? list[index] : defaultValue;
        }
        #endregion

        #region String Extensions
        /// <summary>
        /// Check if the string is null or empty.
        /// </summary>
        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        /// <summary>
        /// Check if the string is null, empty, or whitespace.
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        /// <summary>
        /// Truncate a string to a maximum length.
        /// </summary>
        public static string Truncate(this string str, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(str) || str.Length <= maxLength)
            {
                return str;
            }
            return str.Substring(0, maxLength - suffix.Length) + suffix;
        }

        /// <summary>
        /// Convert a string to title case.
        /// </summary>
        public static string ToTitleCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
        }
        #endregion

        #region Float Extensions
        /// <summary>
        /// Remap a value from one range to another.
        /// </summary>
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        /// <summary>
        /// Check if the float is approximately equal to another.
        /// </summary>
        public static bool Approximately(this float a, float b, float tolerance = 0.0001f)
        {
            return Mathf.Abs(a - b) < tolerance;
        }

        /// <summary>
        /// Round to a specified number of decimal places.
        /// </summary>
        public static float RoundTo(this float value, int decimals)
        {
            return (float)Math.Round(value, decimals);
        }
        #endregion

        #region Int Extensions
        /// <summary>
        /// Format an integer with thousand separators.
        /// </summary>
        public static string ToFormattedString(this int value)
        {
            return value.ToString("N0");
        }

        /// <summary>
        /// Format a large number with K/M/B suffixes.
        /// </summary>
        public static string ToShortString(this int value)
        {
            if (value >= 1000000000)
                return (value / 1000000000f).ToString("0.#") + "B";
            if (value >= 1000000)
                return (value / 1000000f).ToString("0.#") + "M";
            if (value >= 1000)
                return (value / 1000f).ToString("0.#") + "K";
            return value.ToString();
        }
        #endregion

        #region DateTime Extensions
        /// <summary>
        /// Get a relative time string (e.g., "5 minutes ago").
        /// </summary>
        public static string ToRelativeTime(this DateTime dateTime)
        {
            TimeSpan diff = DateTime.Now - dateTime;

            if (diff.TotalSeconds < 60)
                return "just now";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} minutes ago";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} hours ago";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} days ago";
            if (diff.TotalDays < 30)
                return $"{(int)(diff.TotalDays / 7)} weeks ago";
            if (diff.TotalDays < 365)
                return $"{(int)(diff.TotalDays / 30)} months ago";

            return $"{(int)(diff.TotalDays / 365)} years ago";
        }

        /// <summary>
        /// Check if a date is today.
        /// </summary>
        public static bool IsToday(this DateTime dateTime)
        {
            return dateTime.Date == DateTime.Today;
        }
        #endregion

        #region Component Extensions
        /// <summary>
        /// Enable or disable a component.
        /// </summary>
        public static void SetEnabled(this Behaviour behaviour, bool enabled)
        {
            behaviour.enabled = enabled;
        }
        #endregion

        #region RectTransform Extensions
        /// <summary>
        /// Set the anchor to a preset position.
        /// </summary>
        public static void SetAnchorPreset(this RectTransform rectTransform, AnchorPreset preset)
        {
            switch (preset)
            {
                case AnchorPreset.TopLeft:
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(0, 1);
                    break;
                case AnchorPreset.TopCenter:
                    rectTransform.anchorMin = new Vector2(0.5f, 1);
                    rectTransform.anchorMax = new Vector2(0.5f, 1);
                    break;
                case AnchorPreset.TopRight:
                    rectTransform.anchorMin = new Vector2(1, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    break;
                case AnchorPreset.MiddleLeft:
                    rectTransform.anchorMin = new Vector2(0, 0.5f);
                    rectTransform.anchorMax = new Vector2(0, 0.5f);
                    break;
                case AnchorPreset.MiddleCenter:
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    break;
                case AnchorPreset.MiddleRight:
                    rectTransform.anchorMin = new Vector2(1, 0.5f);
                    rectTransform.anchorMax = new Vector2(1, 0.5f);
                    break;
                case AnchorPreset.BottomLeft:
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(0, 0);
                    break;
                case AnchorPreset.BottomCenter:
                    rectTransform.anchorMin = new Vector2(0.5f, 0);
                    rectTransform.anchorMax = new Vector2(0.5f, 0);
                    break;
                case AnchorPreset.BottomRight:
                    rectTransform.anchorMin = new Vector2(1, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    break;
                case AnchorPreset.StretchAll:
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    break;
            }
        }

        public enum AnchorPreset
        {
            TopLeft,
            TopCenter,
            TopRight,
            MiddleLeft,
            MiddleCenter,
            MiddleRight,
            BottomLeft,
            BottomCenter,
            BottomRight,
            StretchAll
        }
        #endregion
    }
}


#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Editor
{
    public static class SerializableDictionaryDrawer
    {
        public const int DefaultPageSize = 20;
        private const float BoxPadding = 6f;
        private const float PagingButtonWidth = 52f;
        private const float AddButtonWidth = 40f;

        public readonly struct ViewMetrics
        {
            public ViewMetrics(int totalCount, int visibleCount, int pageRowCount)
            {
                TotalCount = totalCount;
                VisibleCount = visibleCount;
                PageRowCount = pageRowCount;
            }

            public int TotalCount { get; }
            public int VisibleCount { get; }
            public int PageRowCount { get; }
        }

        public sealed class State
        {
            public string SearchFilter = string.Empty;
            public Vector2 Scroll;
            public int Page;
            private string _lastSearchFilter = string.Empty;

            internal void SyncSearchFilter()
            {
                string filter = SearchFilter?.Trim() ?? string.Empty;
                if (!string.Equals(_lastSearchFilter, filter, StringComparison.Ordinal))
                {
                    Page = 0;
                    _lastSearchFilter = filter;
                }
            }
        }

        public sealed class Options
        {
            public int PageSize = DefaultPageSize;
            public float KeyColumnRatio = 0.42f;
            public float MinKeyColumnWidth = 140f;
            public float MaxScrollHeight = 240f;
            public float RemoveButtonWidth = 20f;
            public string KeyColumnLabel = "Key";
            public string ValueColumnLabel = "Value";
            public string AddEntryLabel = "Add";
            public string RemoveEntryLabel = "-";
            public bool ShowSearch = true;
            public bool ShowPaging = true;
            public bool ShowAddRemove = true;
        }

        private static readonly Dictionary<string, State> PropertyStates = new();

        public static State GetOrCreateState(SerializedProperty property)
        {
            string key = property.propertyPath;
            if (!PropertyStates.TryGetValue(key, out State state))
            {
                state = new State();
                PropertyStates[key] = state;
            }

            return state;
        }

        public static ViewMetrics GetViewMetrics(
            SerializedProperty dictionaryProperty,
            State state,
            Options options = null)
        {
            options ??= new Options();
            state ??= new State();
            state.SyncSearchFilter();

            if (dictionaryProperty == null)
            {
                return new ViewMetrics(0, 0, 0);
            }

            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative("_keys");
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative("_values");

            if (keysProperty == null || valuesProperty == null)
            {
                return new ViewMetrics(0, 0, 0);
            }

            string filter = state.SearchFilter?.Trim() ?? string.Empty;
            int count = Mathf.Min(keysProperty.arraySize, valuesProperty.arraySize);
            List<int> visibleIndices = BuildVisibleIndices(keysProperty, valuesProperty, count, filter);

            int totalPages = Mathf.Max(1, Mathf.CeilToInt(visibleIndices.Count / (float)options.PageSize));
            state.Page = Mathf.Clamp(state.Page, 0, totalPages - 1);

            int pageStart = state.Page * options.PageSize;
            int pageEnd = Mathf.Min(pageStart + options.PageSize, visibleIndices.Count);

            return new ViewMetrics(count, visibleIndices.Count, Mathf.Max(0, pageEnd - pageStart));
        }

        public static float GetDrawHeight(
            SerializedProperty dictionaryProperty,
            State state,
            Options options = null)
        {
            options ??= new Options();
            state ??= new State();
            state.SyncSearchFilter();

            SerializedProperty keysProperty = dictionaryProperty?.FindPropertyRelative("_keys");
            SerializedProperty valuesProperty = dictionaryProperty?.FindPropertyRelative("_values");

            if (keysProperty == null || valuesProperty == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            int chromeLines = (options.ShowSearch ? 1 : 0) + 2;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float lineHeight = EditorGUIUtility.singleLineHeight;

            ViewMetrics metrics = GetViewMetrics(dictionaryProperty, state, options);
            List<int> visibleIndices = BuildVisibleIndices(
                keysProperty,
                valuesProperty,
                metrics.TotalCount,
                state.SearchFilter?.Trim() ?? string.Empty);

            int pageStart = state.Page * options.PageSize;
            int pageEnd = Mathf.Min(pageStart + options.PageSize, visibleIndices.Count);
            float scrollHeight = GetScrollHeight(valuesProperty, visibleIndices, pageStart, pageEnd);
            scrollHeight = Mathf.Min(options.MaxScrollHeight, scrollHeight);

            return BoxPadding * 2f
                + spacing * chromeLines
                + lineHeight * chromeLines
                + scrollHeight;
        }

        public static float GetPropertyHeight(
            SerializedProperty dictionaryProperty,
            State state,
            Options options = null)
        {
            return EditorGUIUtility.singleLineHeight
                + EditorGUIUtility.standardVerticalSpacing
                + GetDrawHeight(dictionaryProperty, state, options);
        }

        public static void Draw(
            SerializedProperty dictionaryProperty,
            State state,
            Options options = null)
        {
            options ??= new Options();
            state ??= new State();

            if (dictionaryProperty == null)
            {
                EditorGUILayout.HelpBox("Missing dictionary property.", MessageType.Error);
                return;
            }

            bool enabled = ForceEnabled();
            DrawProperty(
                GUILayoutUtility.GetRect(
                    0f,
                    GetDrawHeight(dictionaryProperty, state, options),
                    GUILayout.ExpandWidth(true)),
                dictionaryProperty,
                state,
                options);
            RestoreEnabled(enabled);
        }

        public static void DrawProperty(
            Rect position,
            SerializedProperty dictionaryProperty,
            State state,
            Options options = null)
        {
            options ??= new Options();
            state ??= new State();
            state.SyncSearchFilter();

            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative("_keys");
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative("_values");

            if (keysProperty == null || valuesProperty == null)
            {
                EditorGUI.HelpBox(position, "Missing dictionary key/value lists.", MessageType.Error);
                return;
            }

            GUI.Box(position, GUIContent.none, EditorStyles.helpBox);

            Rect rect = new Rect(
                position.x + BoxPadding,
                position.y + BoxPadding,
                position.width - BoxPadding * 2f,
                position.height - BoxPadding * 2f);

            if (options.ShowSearch)
            {
                Rect searchRect = TakeLine(ref rect);
                state.SearchFilter = EditorGUI.TextField(searchRect, "Search", state.SearchFilter);
                state.SyncSearchFilter();
            }

            ViewMetrics metrics = GetViewMetrics(dictionaryProperty, state, options);
            List<int> visibleIndices = BuildVisibleIndices(
                keysProperty,
                valuesProperty,
                metrics.TotalCount,
                state.SearchFilter?.Trim() ?? string.Empty);

            float keyWidth = GetKeyWidth(rect.width, options);
            DrawHeaderRect(TakeLine(ref rect), keyWidth, options);

            int pageStart = state.Page * options.PageSize;
            int pageEnd = Mathf.Min(pageStart + options.PageSize, visibleIndices.Count);
            int pageRowCount = Mathf.Max(0, pageEnd - pageStart);
            float scrollHeight = GetScrollHeight(valuesProperty, visibleIndices, pageStart, pageEnd);
            scrollHeight = Mathf.Min(options.MaxScrollHeight, scrollHeight);
            Rect scrollRect = new Rect(rect.x, rect.y, rect.width, scrollHeight);

            DrawRowsScrollView(
                scrollRect,
                dictionaryProperty,
                keysProperty,
                valuesProperty,
                visibleIndices,
                pageStart,
                pageEnd,
                keyWidth,
                state,
                options);

            rect.y += scrollHeight + EditorGUIUtility.standardVerticalSpacing;

            string filter = state.SearchFilter?.Trim() ?? string.Empty;
            DrawFooterRect(
                TakeLine(ref rect),
                filter,
                metrics,
                dictionaryProperty,
                state,
                options);
        }

        private static float GetRowContentWidth(Rect scrollRect, float contentHeight)
        {
            float width = scrollRect.width;
            if (contentHeight > scrollRect.height + 0.5f)
            {
                width -= GUI.skin.verticalScrollbar.fixedWidth;
            }

            return Mathf.Max(0f, width);
        }

        private static float GetKeyWidth(float totalWidth, Options options)
        {
            return Mathf.Max(options.MinKeyColumnWidth, totalWidth * options.KeyColumnRatio);
        }

        private static Rect TakeLine(ref Rect rect)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect line = new Rect(rect.x, rect.y, rect.width, lineHeight);
            rect.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
            return line;
        }

        private static void DrawFooterRect(
            Rect rect,
            string filter,
            ViewMetrics metrics,
            SerializedProperty dictionaryProperty,
            State state,
            Options options)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            string footerText = BuildFooterText(filter, metrics.VisibleCount, metrics.TotalCount, state, options);

            int totalPages = Mathf.Max(1, Mathf.CeilToInt(metrics.VisibleCount / (float)options.PageSize));
            state.Page = Mathf.Clamp(state.Page, 0, totalPages - 1);

            float rightEdge = rect.xMax;
            Rect nextRect = default;
            Rect prevRect = default;
            Rect addRect = default;

            if (options.ShowPaging)
            {
                nextRect = new Rect(rightEdge - PagingButtonWidth, rect.y, PagingButtonWidth, lineHeight);
                rightEdge = nextRect.x - 2f;
                prevRect = new Rect(rightEdge - PagingButtonWidth, rect.y, PagingButtonWidth, lineHeight);
                rightEdge = prevRect.x - 2f;
            }

            if (options.ShowAddRemove)
            {
                addRect = new Rect(rightEdge - AddButtonWidth, rect.y, AddButtonWidth, lineHeight);
                rightEdge = addRect.x - 4f;
            }

            Rect labelRect = new Rect(rect.x, rect.y, Mathf.Max(0f, rightEdge - rect.x), lineHeight);
            EditorGUI.LabelField(labelRect, footerText, EditorStyles.miniLabel);

            if (options.ShowAddRemove)
            {
                using (new EditorGUI.DisabledScope(!GUI.enabled))
                {
                    if (GUI.Button(addRect, options.AddEntryLabel, EditorStyles.miniButton))
                    {
                        AddEntry(dictionaryProperty, state, options);
                    }
                }
            }

            if (options.ShowPaging)
            {
                using (new EditorGUI.DisabledScope(state.Page <= 0))
                {
                    if (GUI.Button(prevRect, "Prev", EditorStyles.miniButtonLeft))
                    {
                        state.Page--;
                    }
                }

                using (new EditorGUI.DisabledScope(state.Page >= totalPages - 1))
                {
                    if (GUI.Button(nextRect, "Next", EditorStyles.miniButtonRight))
                    {
                        state.Page++;
                    }
                }
            }
        }

        private static void DrawHeaderRect(Rect rect, float keyWidth, Options options)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float removeWidth = options.ShowAddRemove ? options.RemoveButtonWidth : 0f;
            EditorGUI.LabelField(
                new Rect(rect.x, rect.y, keyWidth, lineHeight),
                options.KeyColumnLabel,
                EditorStyles.boldLabel);
            EditorGUI.LabelField(
                new Rect(rect.x + keyWidth, rect.y, rect.width - keyWidth - removeWidth, lineHeight),
                options.ValueColumnLabel,
                EditorStyles.boldLabel);
        }

        private static void DrawRowsScrollView(
            Rect scrollRect,
            SerializedProperty dictionaryProperty,
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty,
            List<int> visibleIndices,
            int pageStart,
            int pageEnd,
            float keyWidth,
            State state,
            Options options)
        {
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float contentHeight = GetScrollHeight(valuesProperty, visibleIndices, pageStart, pageEnd);
            float contentWidth = GetRowContentWidth(scrollRect, contentHeight);
            float removeWidth = options.ShowAddRemove ? options.RemoveButtonWidth : 0f;
            float valueWidth = contentWidth - keyWidth - removeWidth;

            Rect viewRect = new Rect(0f, 0f, contentWidth, contentHeight);
            state.Scroll = GUI.BeginScrollView(scrollRect, state.Scroll, viewRect);

            float y = spacing;
            float keyLineHeight = EditorGUIUtility.singleLineHeight;
            for (int visibleIndex = pageStart; visibleIndex < pageEnd; visibleIndex++)
            {
                int arrayIndex = visibleIndices[visibleIndex];
                SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(arrayIndex);
                SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(arrayIndex);
                float valueHeight = GetValueHeight(valueProperty);
                float rowHeight = Mathf.Max(keyLineHeight, valueHeight);

                EditorGUI.PropertyField(
                    new Rect(0f, y, keyWidth, keyLineHeight),
                    keyProperty,
                    GUIContent.none);
                EditorGUI.PropertyField(
                    new Rect(keyWidth, y, valueWidth, valueHeight),
                    valueProperty,
                    GUIContent.none,
                    true);

                if (options.ShowAddRemove)
                {
                    Rect removeRect = new Rect(keyWidth + valueWidth, y, removeWidth, keyLineHeight);
                    using (new EditorGUI.DisabledScope(!GUI.enabled))
                    {
                        if (GUI.Button(removeRect, options.RemoveEntryLabel, EditorStyles.miniButton))
                        {
                            RemoveEntry(dictionaryProperty, state, options, arrayIndex);
                            GUI.EndScrollView();
                            return;
                        }
                    }
                }

                y += rowHeight + spacing;
            }

            GUI.EndScrollView();
        }

        private static void AddEntry(
            SerializedProperty dictionaryProperty,
            State state,
            Options options)
        {
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative("_keys");
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative("_values");
            if (keysProperty == null || valuesProperty == null)
            {
                return;
            }

            SerializedObject serializedObject = dictionaryProperty.serializedObject;
            serializedObject.Update();
            RecordUndo(serializedObject, "Add Dictionary Entry");

            int index = keysProperty.arraySize;
            keysProperty.arraySize++;
            valuesProperty.arraySize++;

            AssignDefaultKey(keysProperty.GetArrayElementAtIndex(index), keysProperty, index);
            ResetPropertyToDefault(valuesProperty.GetArrayElementAtIndex(index));

            serializedObject.ApplyModifiedProperties();

            int visibleCount = BuildVisibleIndices(
                keysProperty,
                valuesProperty,
                keysProperty.arraySize,
                state.SearchFilter?.Trim() ?? string.Empty).Count;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(visibleCount / (float)options.PageSize));
            state.Page = totalPages - 1;
        }

        private static void RemoveEntry(
            SerializedProperty dictionaryProperty,
            State state,
            Options options,
            int arrayIndex)
        {
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative("_keys");
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative("_values");
            if (keysProperty == null ||
                valuesProperty == null ||
                arrayIndex < 0 ||
                arrayIndex >= keysProperty.arraySize ||
                arrayIndex >= valuesProperty.arraySize)
            {
                return;
            }

            SerializedObject serializedObject = dictionaryProperty.serializedObject;
            serializedObject.Update();
            RecordUndo(serializedObject, "Remove Dictionary Entry");

            keysProperty.DeleteArrayElementAtIndex(arrayIndex);
            valuesProperty.DeleteArrayElementAtIndex(arrayIndex);

            serializedObject.ApplyModifiedProperties();

            int visibleCount = BuildVisibleIndices(
                keysProperty,
                valuesProperty,
                keysProperty.arraySize,
                state.SearchFilter?.Trim() ?? string.Empty).Count;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(visibleCount / (float)options.PageSize));
            state.Page = Mathf.Clamp(state.Page, 0, totalPages - 1);
        }

        private static void RecordUndo(SerializedObject serializedObject, string actionName)
        {
            UnityEngine.Object[] targets = serializedObject.targetObjects;
            if (targets == null || targets.Length == 0)
            {
                return;
            }

            if (targets.Length == 1)
            {
                Undo.RecordObject(targets[0], actionName);
                return;
            }

            Undo.RecordObjects(targets, actionName);
        }

        private static void AssignDefaultKey(
            SerializedProperty keyProperty,
            SerializedProperty keysProperty,
            int newIndex)
        {
            if (keyProperty.propertyType == SerializedPropertyType.String)
            {
                keyProperty.stringValue = GenerateUniqueStringKey(keysProperty, newIndex);
                return;
            }

            ResetPropertyToDefault(keyProperty);
        }

        private static string GenerateUniqueStringKey(SerializedProperty keysProperty, int skipIndex)
        {
            const string baseName = "NewEntry";
            var usedKeys = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < keysProperty.arraySize; i++)
            {
                if (i == skipIndex)
                {
                    continue;
                }

                SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(i);
                if (keyProperty.propertyType != SerializedPropertyType.String)
                {
                    continue;
                }

                string key = keyProperty.stringValue;
                if (!string.IsNullOrEmpty(key))
                {
                    usedKeys.Add(key);
                }
            }

            if (!usedKeys.Contains(baseName))
            {
                return baseName;
            }

            for (int suffix = 1; suffix < int.MaxValue; suffix++)
            {
                string candidate = $"{baseName}_{suffix}";
                if (!usedKeys.Contains(candidate))
                {
                    return candidate;
                }
            }

            return baseName;
        }

        private static void ResetPropertyToDefault(SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                    property.stringValue = string.Empty;
                    return;
                case SerializedPropertyType.Character:
                    property.intValue = 0;
                    return;
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum:
                    property.intValue = 0;
                    return;
                case SerializedPropertyType.Boolean:
                    property.boolValue = false;
                    return;
                case SerializedPropertyType.Float:
                    property.floatValue = 0f;
                    return;
                case SerializedPropertyType.Color:
                    property.colorValue = Color.white;
                    return;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = null;
                    return;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = Vector2.zero;
                    return;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = Vector3.zero;
                    return;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = Vector4.zero;
                    return;
                case SerializedPropertyType.Rect:
                    property.rectValue = Rect.zero;
                    return;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = AnimationCurve.Constant(0f, 1f, 0f);
                    return;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = new Bounds(Vector3.zero, Vector3.one);
                    return;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = Quaternion.identity;
                    return;
                case SerializedPropertyType.ExposedReference:
                    property.exposedReferenceValue = null;
                    return;
                case SerializedPropertyType.ManagedReference:
                    property.managedReferenceValue = null;
                    return;
            }

            if (!property.hasVisibleChildren)
            {
                return;
            }

            SerializedProperty iterator = property.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();
            iterator.NextVisible(true);

            while (!SerializedProperty.EqualContents(iterator, endProperty))
            {
                ResetPropertyToDefault(iterator);
                if (!iterator.NextVisible(false))
                {
                    break;
                }
            }
        }

        private static float GetScrollHeight(
            SerializedProperty valuesProperty,
            List<int> visibleIndices,
            int pageStart,
            int pageEnd)
        {
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            if (pageEnd <= pageStart || valuesProperty == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float contentHeight = spacing;
            float keyLineHeight = EditorGUIUtility.singleLineHeight;
            for (int visibleIndex = pageStart; visibleIndex < pageEnd; visibleIndex++)
            {
                int arrayIndex = visibleIndices[visibleIndex];
                SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(arrayIndex);
                float rowHeight = Mathf.Max(keyLineHeight, GetValueHeight(valueProperty));
                contentHeight += rowHeight + spacing;
            }

            return contentHeight;
        }

        private static float GetValueHeight(SerializedProperty valueProperty) =>
            valueProperty == null
                ? EditorGUIUtility.singleLineHeight
                : EditorGUI.GetPropertyHeight(valueProperty, GUIContent.none, true);

        private static List<int> BuildVisibleIndices(
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty,
            int count,
            string filter)
        {
            var visibleIndices = new List<int>(count);

            for (int i = 0; i < count; i++)
            {
                SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(i);
                SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(i);

                if (filter.Length > 0 &&
                    GetSearchableText(keyProperty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                    GetSearchableText(valueProperty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                visibleIndices.Add(i);
            }

            return visibleIndices;
        }

        private static string BuildFooterText(
            string filter,
            int visibleCount,
            int totalCount,
            State state,
            Options options)
        {
            string rangeText = string.Empty;
            if (options.ShowPaging && visibleCount > 0)
            {
                int pageStart = state.Page * options.PageSize;
                int pageEnd = Mathf.Min(pageStart + options.PageSize, visibleCount);
                rangeText = $" · showing {pageStart + 1}-{pageEnd}";
            }

            if (filter.Length > 0)
            {
                return $"Showing {visibleCount} of {totalCount} entries{rangeText}";
            }

            return $"{totalCount} entries{rangeText}";
        }

        private static string GetSearchableText(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                    return property.stringValue ?? string.Empty;
                case SerializedPropertyType.Integer:
                    return property.intValue.ToString();
                case SerializedPropertyType.Enum:
                    return property.enumDisplayNames[property.enumValueIndex];
                case SerializedPropertyType.Boolean:
                    return property.boolValue.ToString();
                case SerializedPropertyType.Float:
                    return property.floatValue.ToString("G");
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue != null
                        ? property.objectReferenceValue.name
                        : string.Empty;
                default:
                    return property.displayName ?? string.Empty;
            }
        }

        private static bool ForceEnabled()
        {
            bool enabled = GUI.enabled;
            GUI.enabled = true;
            return enabled;
        }

        private static void RestoreEnabled(bool enabled) => GUI.enabled = enabled;
    }
}
#endif

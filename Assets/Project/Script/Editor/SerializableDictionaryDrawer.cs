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
            public string KeyColumnLabel = "Key";
            public string ValueColumnLabel = "Value";
            public bool ShowSearch = true;
            public bool ShowPaging = true;
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
            ViewMetrics metrics = GetViewMetrics(dictionaryProperty, state, options);

            int chromeLines = (options.ShowSearch ? 1 : 0) + 2;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float lineHeight = EditorGUIUtility.singleLineHeight;

            return BoxPadding * 2f
                + spacing * chromeLines
                + lineHeight * chromeLines
                + GetScrollHeight(metrics.PageRowCount, options.MaxScrollHeight);
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
            float scrollHeight = GetScrollHeight(pageRowCount, options.MaxScrollHeight);
            Rect scrollRect = new Rect(rect.x, rect.y, rect.width, scrollHeight);

            DrawRowsScrollView(
                scrollRect,
                keysProperty,
                valuesProperty,
                visibleIndices,
                pageStart,
                pageEnd,
                keyWidth,
                state);

            rect.y += scrollHeight + EditorGUIUtility.standardVerticalSpacing;

            string filter = state.SearchFilter?.Trim() ?? string.Empty;
            DrawFooterRect(
                TakeLine(ref rect),
                filter,
                metrics,
                state,
                options);
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

        private static void DrawHeaderRect(Rect rect, float keyWidth, Options options)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(
                new Rect(rect.x, rect.y, keyWidth, lineHeight),
                options.KeyColumnLabel,
                EditorStyles.boldLabel);
            EditorGUI.LabelField(
                new Rect(rect.x + keyWidth, rect.y, rect.width - keyWidth, lineHeight),
                options.ValueColumnLabel,
                EditorStyles.boldLabel);
        }

        private static void DrawFooterRect(
            Rect rect,
            string filter,
            ViewMetrics metrics,
            State state,
            Options options)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            string footerText = BuildFooterText(filter, metrics.VisibleCount, metrics.TotalCount, state, options);

            if (!options.ShowPaging)
            {
                EditorGUI.LabelField(rect, footerText, EditorStyles.miniLabel);
                return;
            }

            int totalPages = Mathf.Max(1, Mathf.CeilToInt(metrics.VisibleCount / (float)options.PageSize));
            state.Page = Mathf.Clamp(state.Page, 0, totalPages - 1);

            Rect nextRect = new Rect(rect.xMax - PagingButtonWidth, rect.y, PagingButtonWidth, lineHeight);
            Rect prevRect = new Rect(nextRect.x - PagingButtonWidth - 2f, rect.y, PagingButtonWidth, lineHeight);
            Rect labelRect = new Rect(rect.x, rect.y, prevRect.x - rect.x - 4f, lineHeight);

            EditorGUI.LabelField(labelRect, footerText, EditorStyles.miniLabel);

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

        private static void DrawRowsScrollView(
            Rect scrollRect,
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty,
            List<int> visibleIndices,
            int pageStart,
            int pageEnd,
            float keyWidth,
            State state)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            int rowCount = Mathf.Max(0, pageEnd - pageStart);
            float contentHeight = rowCount > 0
                ? rowCount * lineHeight + Mathf.Max(0, rowCount - 1) * spacing + spacing
                : lineHeight;

            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, contentHeight);
            state.Scroll = GUI.BeginScrollView(scrollRect, state.Scroll, viewRect);

            float y = 0f;
            for (int visibleIndex = pageStart; visibleIndex < pageEnd; visibleIndex++)
            {
                int arrayIndex = visibleIndices[visibleIndex];
                SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(arrayIndex);
                SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(arrayIndex);

                EditorGUI.PropertyField(
                    new Rect(0f, y, keyWidth, lineHeight),
                    keyProperty,
                    GUIContent.none);
                EditorGUI.PropertyField(
                    new Rect(keyWidth, y, viewRect.width - keyWidth, lineHeight),
                    valueProperty,
                    GUIContent.none);

                y += lineHeight + spacing;
            }

            GUI.EndScrollView();
        }

        private static float GetScrollHeight(int rowCount, float maxScrollHeight)
        {
            float rowHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (rowCount <= 0)
            {
                return rowHeight;
            }

            float contentHeight = rowCount * rowHeight + EditorGUIUtility.standardVerticalSpacing;
            return Mathf.Min(maxScrollHeight, contentHeight);
        }

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

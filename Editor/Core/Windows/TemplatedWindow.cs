﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThunderKit.Common;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Text.RegularExpressions;
#if UNITY_2019_1_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#elif UNITY_2018_1_OR_NEWER
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
#endif

namespace ThunderKit.Core.Editor.Windows
{
    using static ThunderKit.Core.UIElements.TemplateHelpers;

    public abstract class TemplatedWindow : EditorWindow
    {
#if UNITY_2019_1_OR_NEWER
#elif UNITY_2018_1_OR_NEWER
        PropertyInfo rvcField;
        VisualElement rvc;
        protected VisualElement rootVisualElement
        {
            get
            {
                if (rvcField == null)
                    rvcField = typeof(EditorWindow)
                               .GetProperty("rootVisualContainer", BindingFlags.NonPublic | BindingFlags.Instance);

                if (rvc == null)
                    rvc = rvcField.GetValue(this) as VisualElement;

                return rvc;
            }
        }
#endif

        [SerializeField] public Texture2D ThunderKitIcon;

        public virtual void OnEnable()
        {
            rootVisualElement.Clear();
            GetTemplateInstance(GetType().Name, rootVisualElement);
            rootVisualElement.Bind(new SerializedObject(this));
        }
    }
}
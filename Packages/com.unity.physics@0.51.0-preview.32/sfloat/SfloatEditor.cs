#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using DG.DemiEditor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace UnityS.Mathematics
{
	#region --> old

	//	public static class MyExtensions
	//	{
	//#if UNITY_EDITOR
	//		// Gets value from SerializedProperty - even if value is nested
	//		public static object GetValue(this UnityEditor.SerializedProperty property) {
	//			object obj = property.serializedObject.targetObject;
	//			FieldInfo field = null;
	//			foreach (var path in property.propertyPath.Split('.')) {
	//				var type = obj?.GetType();
	//				field = type?.GetField(path);
	//				obj = field?.GetValue(obj);
	//			}
	//
	//			return obj;
	//		}
	//
	//		// Sets value from SerializedProperty - even if value is nested
	//		public static void SetValue(this UnityEditor.SerializedProperty property, object val) {
	//			object obj = property.serializedObject.targetObject;
	//			List<KeyValuePair<FieldInfo, object>> list = new List<KeyValuePair<FieldInfo, object>>();
	//			FieldInfo field = null;
	//			foreach (var path in property.propertyPath.Split('.')) {
	//				var type = obj.GetType();
	//				field = type.GetField(path);
	//				list.Add(new KeyValuePair<FieldInfo, object>(field, obj));
	//				obj = field.GetValue(obj);
	//			}
	//
	//			// Now set values of all objects, from child to parent
	//			for (int i = list.Count - 1; i >= 0; --i) {
	//				list[i].Key.SetValue(list[i].Value, val);
	//				// New 'val' object will be parent of current 'val' object
	//				val = list[i].Value;
	//			}
	//		}
	//#endif // UNITY_EDITOR
	//	}

	#endregion

	[CustomPropertyDrawer(typeof(sfloat))]
	public class SfloatEditor : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			//base.OnGUI(position,property,label);
			EditorGUI.BeginProperty(position, label, property);
//			SerializedProperty value = null;
//			if (value == null)
//			{
//				property.Next(true);
//				value = property.Copy();
//			}
//	// now we can actually assign the value
//			value.doubleValue = EditorGUI.DoubleField(position, label, value.doubleValue);
//			EditorGUI.EndProperty();
			EditorGUI.BeginChangeCheck();
			//var tFloat = property.FindPropertyRelative("floatValue");
			var tInt = property.FindPropertyRelative("rawValue");
			var value = sfloat.ToFloat((uint) tInt.longValue);
			//autos to the default drawer for _value, ie. a doubleField
			var newValue = EditorGUI.FloatField(position, label, value);
			if (EditorGUI.EndChangeCheck()) {
				tInt.longValue = sfloat.ToInt(newValue);
				//tFloat.floatValue = 0;
				//property.serializedObject.ApplyModifiedProperties();
			}

			EditorGUI.EndProperty();
		}
	}
}

#endif

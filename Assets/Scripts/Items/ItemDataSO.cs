using System.Collections.Generic;
using UnityEngine;

// per-item config — icon, roll range, single-use, duplicate conversion
[CreateAssetMenu(fileName ="ItemDataSO", menuName = "ScriptableObjects/Item")]
public class ItemDataSO : ScriptableObject
{ 
	public ItemTypes TypeId; 

	public Sprite Icon;
	public string Name;
	public string CardDisplayTextTitle;
	public string CardDisplayTextDescription;
	[Space(10)]
	public bool SingleUse;

	[Space(10)]
	public ItemDataSO ConvertIntoIfAlreadyHaveIt; // duplicate single-use → this instead
	public int ConvertAmount;

	[Space(10)]
	public int MinAmount;        // wheel roll min
	public int MaxAmount;        // wheel roll max (inclusive)
	public int MultiplierAmount; // pile bonus per zone tier chunk

	#region EqualOverrides
	// lets you compare ItemDataSO == ItemTypes in conditionals
	public static bool operator==(ItemDataSO left, ItemTypes right)
	{
		return left.TypeId == right;
	}

	public static bool operator!=(ItemDataSO left, ItemTypes right) => !(left == right);

	public bool Equals(ItemTypes other) => TypeId == other;

	public override bool Equals(object other)
	{
		if (other is ItemTypes t)
			return TypeId == t;
		if (other is ItemDataSO o && (Object)o != null)
			return TypeId == o.TypeId;
		return false;
	}
	
	public override int GetHashCode() => (int)TypeId;
	
#endregion EqualOverrides

}

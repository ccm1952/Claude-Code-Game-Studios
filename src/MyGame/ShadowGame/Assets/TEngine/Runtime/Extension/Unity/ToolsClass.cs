using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

public static class ToolsClass
{

    #region UnitySelfExpand
    #region UI

    #region RectTransform
    #region AnchoredPosition
    /// <summary>
    /// Set anchoredPosition via a vector coordinate
    /// </summary>
    /// <param name="self"></param>
    /// <param name="anchoredPosition">Coordinate position</param>
    /// <returns></returns>
    public static RectTransform AnchoredPosition(this RectTransform self, Vector3 anchoredPosition)
    {
        self.anchoredPosition = anchoredPosition;
        return self;
    }
    /// <summary>
    /// Set anchoredPosition via an X coordinate and a Y coordinate
    /// </summary>
    /// <param name="self"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static RectTransform AnchoredPosition(this RectTransform self, float x, float y)
    {
        Vector2 anchoredPosition = self.anchoredPosition;
        anchoredPosition.x = x;
        anchoredPosition.y = y;
        self.anchoredPosition = anchoredPosition;
        return self;
    }
    /// <summary>
    /// Set anchoredPosition X coordinate value.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    public static RectTransform AnchoredPositionX(this RectTransform self, float x)
    {
        Vector2 anchoredPosition = self.anchoredPosition;
        anchoredPosition.x = x;
        self.anchoredPosition = anchoredPosition;
        return self;
    }
    /// <summary>
    /// Set anchoredPosition Y coordinate value.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="y">Y</param>
    /// <returns></returns>
    public static RectTransform AnchoredPositionY(this RectTransform self, float y)
    {
        Vector2 anchoredPosition = self.anchoredPosition;
        anchoredPosition.y = y;
        self.anchoredPosition = anchoredPosition;
        return self;
    }
    #endregion
    #region OffsetMax
    /// <summary>
    /// Set the offset max.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="offsetMax"></param>
    /// <returns></returns>
    public static RectTransform OffsetMax(this RectTransform self, Vector2 offsetMax)
    {
        self.offsetMax = offsetMax;
        return self;
    }
    /// <summary>
    /// Set the offset max.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static RectTransform OffsetMax(this RectTransform self, float x, float y)
    {
        Vector2 offsetMax = self.offsetMax;
        offsetMax.x = x;
        offsetMax.y = y;
        self.offsetMax = offsetMax;
        return self;
    }
    /// <summary>
    /// Set the offset max x value.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    public static RectTransform OffsetMaxX(this RectTransform self, float x)
    {
        Vector2 offsetMax = self.offsetMax;
        offsetMax.x = x;
        self.offsetMax = offsetMax;
        return self;
    }
    /// <summary>
    /// Set the offset max y value.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static RectTransform OffsetMaxY(this RectTransform self, float y)
    {
        Vector2 offsetMax = self.offsetMax;
        offsetMax.y = y;
        self.offsetMax = offsetMax;
        return self;
    }
    #endregion
    #region OffsetMin
    /// <summary>
    /// Set the offset min.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="offsetMin"></param>
    /// <returns></returns>
    public static RectTransform OffsetMin(this RectTransform self, Vector2 offsetMin)
    {
        self.offsetMin = offsetMin;
        return self;
    }
    /// <summary>
    /// Set the offset min.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static RectTransform OffsetMin(this RectTransform self, float x, float y)
    {
        Vector2 offsetMin = self.offsetMin;
        offsetMin.x = x;
        offsetMin.y = y;
        self.offsetMin = offsetMin;
        return self;
    }
    /// <summary>
    /// Set the offset min x value.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    public static RectTransform OffsetMinX(this RectTransform self, float x)
    {
        Vector2 offsetMin = self.offsetMin;
        offsetMin.x = x;
        self.offsetMin = offsetMin;
        return self;
    }
    /// <summary>
    /// Set the offset min y value.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static RectTransform OffsetMinY(this RectTransform self, float y)
    {
        Vector2 offsetMin = self.offsetMin;
        offsetMin.y = y;
        self.offsetMin = offsetMin;
        return self;
    }
    #endregion
    #region AnchoredPosition3D
    /// <summary>
    /// Set the anchored position 3d.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="anchoredPosition3D"></param>
    /// <returns></returns>
    public static RectTransform AnchoredPosition3D(this RectTransform self, Vector2 anchoredPosition3D)
    {
        self.anchoredPosition3D = anchoredPosition3D;
        return self;
    }
    /// <summary>
    /// Set the anchored position 3d.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static RectTransform AnchoredPosition3D(this RectTransform self, float x, float y)
    {
        Vector2 anchoredPosition3D = self.anchoredPosition3D;
        anchoredPosition3D.x = x;
        anchoredPosition3D.y = y;
        self.anchoredPosition3D = anchoredPosition3D;
        return self;
    }
    /// <summary>
    /// Set the anchored position 3d x value.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    public static RectTransform AnchoredPosition3DX(this RectTransform self, float x)
    {
        Vector2 anchoredPosition3D = self.anchoredPosition3D;
        anchoredPosition3D.x = x;
        self.anchoredPosition3D = anchoredPosition3D;
        return self;
    }
    /// <summary>
    /// Set the anchored position 3d y value.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static RectTransform AnchoredPosition3DY(this RectTransform self, float y)
    {
        Vector2 anchoredPosition3D = self.anchoredPosition3D;
        anchoredPosition3D.y = y;
        self.anchoredPosition3D = anchoredPosition3D;
        return self;
    }
    #endregion
    #region AnchorMin
    /// <summary>
    /// Set the anchor min.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="anchorMin"></param>
    /// <returns></returns>
    public static RectTransform AnchorMin(this RectTransform self, Vector2 anchorMin)
    {
        self.anchorMin = anchorMin;
        return self;
    }
    /// <summary>
    /// Set the anchor min.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static RectTransform AnchorMin(this RectTransform self, float x, float y)
    {
        Vector2 anchorMin = self.anchorMin;
        anchorMin.x = x;
        anchorMin.y = y;
        self.anchorMin = anchorMin;
        return self;
    }
    /// <summary>
    /// Set the anchor min x value.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    public static RectTransform AnchorMinX(this RectTransform self, float x)
    {
        Vector2 anchorMin = self.anchorMin;
        anchorMin.x = x;
        self.anchorMin = anchorMin;
        return self;
    }
    /// <summary>
    /// Set the anchor min y value.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static RectTransform AnchorMinY(this RectTransform self, float y)
    {
        Vector2 anchorMin = self.anchorMin;
        anchorMin.y = y;
        self.anchorMin = anchorMin;
        return self;
    }
    #endregion
    #region AnchorMax
    /// <summary>
    /// Set the anchor max.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="anchorMax"></param>
    /// <returns></returns>
    public static RectTransform AnchorMax(this RectTransform self, Vector2 anchorMax)
    {
        self.anchorMax = anchorMax;
        return self;
    }
    /// <summary>
    /// Set the anchor max.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static RectTransform AnchorMax(this RectTransform self, float x, float y)
    {
        Vector2 anchorMax = self.anchorMax;
        anchorMax.x = x;
        anchorMax.y = y;
        self.anchorMax = anchorMax;
        return self;
    }
    /// <summary>
    /// Set the anchor max x value.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    public static RectTransform AnchorMaxX(this RectTransform self, float x)
    {
        Vector2 anchorMax = self.anchorMax;
        anchorMax.x = x;
        self.anchorMax = anchorMax;
        return self;
    }
    /// <summary>
    /// Set the anchor max y value.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static RectTransform AnchorMaxY(this RectTransform self, float y)
    {
        Vector2 anchorMax = self.anchorMax;
        anchorMax.y = y;
        self.anchorMax = anchorMax;
        return self;
    }
    #endregion
    #region Pivot
    /// <summary>
    /// Set the pivot.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="pivot"></param>
    /// <returns></returns>
    public static RectTransform Pivot(this RectTransform self, Vector2 pivot)
    {
        self.pivot = pivot;
        return self;
    }
    /// <summary>
    /// Set the pivot.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static RectTransform Pivot(this RectTransform self, float x, float y)
    {
        Vector2 pivot = self.pivot;
        pivot.x = x;
        pivot.y = y;
        self.pivot = pivot;
        return self;
    }
    /// <summary>
    /// Set the pivot x value.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    public static RectTransform PivotX(this RectTransform self, float x)
    {
        Vector2 pivot = self.pivot;
        pivot.x = x;
        self.pivot = pivot;
        return self;
    }
    /// <summary>
    /// Set the pivot y value.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static RectTransform PivotY(this RectTransform self, float y)
    {
        Vector2 pivot = self.pivot;
        pivot.y = y;
        self.pivot = pivot;
        return self;
    }
    #endregion
    #region SizeDelta
    /// <summary>
    /// Set the size delta.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="sizeDelta"></param>
    /// <returns></returns>
    public static RectTransform SizeDelta(this RectTransform self, Vector2 sizeDelta)
    {
        self.sizeDelta = sizeDelta;
        return self;
    }
    /// <summary>
    /// Set the size delta.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static RectTransform SizeDelta(this RectTransform self, float x, float y)
    {
        Vector2 sizeDelta = self.sizeDelta;
        sizeDelta.x = x;
        sizeDelta.y = y;
        self.sizeDelta = sizeDelta;
        return self;
    }
    /// <summary>
    /// Set the size delta x value.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    public static RectTransform SizeDeltaX(this RectTransform self, float x)
    {
        Vector2 sizeDelta = self.sizeDelta;
        sizeDelta.x = x;
        self.sizeDelta = sizeDelta;
        return self;
    }
    /// <summary>
    /// Set the size delta y value.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static RectTransform SizeDeltaY(this RectTransform self, float y)
    {
        Vector2 sizeDelta = self.sizeDelta;
        sizeDelta.y = y;
        self.sizeDelta = sizeDelta;
        return self;
    }
    #endregion
    #region Size
    /// <summary>
    /// Set width with current anchors.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="width"></param>
    /// <returns></returns>
    public static RectTransform SetSizeWidth(this RectTransform self, float width)
    {
        self.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        return self;
    }
    /// <summary>
    /// Set height with current anchors.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static RectTransform SetSizeHeight(this RectTransform self, float height)
    {
        self.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        return self;
    }

    #endregion
    #endregion

    /// <summary>
    /// Button to add click event
    /// </summary>
    /// <param name="self">Button component of the UI</param>
    /// <param name="action">Parameterless event</param>
    public static void AddClick(this Button self, System.Action action)
    {
        self.onClick.AddListener(() => { action?.Invoke(); });
    }

    /// <summary>
    /// Set the location in the UI
    /// </summary>
    /// <param name="self">UI的Transform</param>
    /// <param name="parentTransform">UI parent object</param>
    /// <param name="screenTargetPos">UI mouse point position</param>
    /// <param name="camera">Camera</param>
    /// <param name="offset">offset</param>
    public static void SetUILocaPos(this Transform self, Transform parentTransform, Vector2 screenTargetPos, Camera camera, Vector2 offset)
    {
        Vector2 locaPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentTransform as RectTransform, screenTargetPos, camera, out locaPos);
        self.localPosition = locaPos + offset;
    }

    #endregion

    /// <summary>
    /// Unknown level search object.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="childName"></param>
    /// <returns></returns>
    public static Transform FindChildTransformByName(this Transform self, string childName)
    {
        Transform c = self.Find(childName);
        if (c != null) return c;
        for (int i = 0; i < self.childCount; i++)
        {
            c = FindChildTransformByName(self.GetChild(i), childName);
            if (c != null) return c;
        }
        return null;
    }

    /// <summary>
    /// The unknown level acquires the components of the object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="childName"></param>
    /// <returns></returns>
    public static T FindChindComponentByName<T>(this Transform self, string childName) where T : class
    {
        Transform tr = self.FindChildTransformByName(childName);
        T t = tr.GetComponent<T>();
        if (t == null)
        {
            TEngine.Log.Error(string.Format("Component not found in {0} {1}", self.name, typeof(T)));
            return null;
        }
        return t;
    }

    /// <summary>
    /// Get all T components of an object (including itself and child objects)
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static List<T> GetMono<T>(this Transform self) where T : MonoBehaviour
    {
        List<T> monoList = new List<T>();

        T mono = self.GetComponent<T>();
        if (mono != null)
        {
            monoList.Add(mono);
        }

        int count = self.childCount;
        for (int i = 0; i < count; i++)
        {
            Transform trc = self.GetChild(i);
            List<T> monoListN = GetMono<T>(trc);
            if (monoListN.Count > 0)
            {
                monoList.AddRange(monoListN);
            }
        }
        return monoList;
    }

    /// <summary>
    /// Clipboard
    /// </summary>
    /// <returns></returns>
    public static string GetClipboard()
    {
        return GUIUtility.systemCopyBuffer;
    }

    /// <summary>
    /// Clipboard
    /// </summary>
    /// <returns></returns>
    public static void SetClipboard(this string value)
    {
        GUIUtility.systemCopyBuffer = value;
    }

    #endregion

    #region An extension to the linked list

    /// <summary>
    /// Selects an element from the list that matches a certain condition
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="func">An element meets a condition</param>
    /// <returns></returns>
    public static T GetOneByList<T>(this List<T> self, System.Func<T, bool> func)
    {
        int count = self.Count;
        for (int i = 0; i < count; i++)
        {
            if (func(self[i]))
            {
                return self[i];
            }
        }
        return default;
    }

    /// <summary>
    /// Get all the elements in a linked list that match a certain condition
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="func">An element meets a condition</param>
    /// <returns></returns>
    public static List<T> GetAllByList<T>(this List<T> self, System.Func<T, bool> func)
    {
        List<T> ts = new List<T>();

        int count = self.Count;
        for (int i = 0; i < count; i++)
        {
            if (func(self[i]))
            {
                ts.Add(self[i]);
            }
        }
        return ts;
    }

    /// <summary>
    /// Selects an element from the array that best matches a certain condition
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="func">An element meets a condition</param>
    /// <returns></returns>
    public static T GetOneByList<T>(this List<T> self, System.Func<T, T, bool> func)
    {
        T t = self[0];
        int count = self.Count;
        for (int i = 1; i < count; i++)
        {
            if (func(t, self[i]))
            {
                t = self[i];
            }
        }
        return t;
    }

    /// <summary>
    /// Shuffle a list at random
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    public static void RandomDisturbList<T>(this List<T> self)
    {
        List<T> oldList = new List<T>();

        int count = self.Count;
        for (int i = 0; i < count; i++)
        {
            oldList.Add(self[i]);
        }

        for (int i = 0; i < count; i++)
        {
            int r = UnityEngine.Random.Range(0, oldList.Count);
            self[i] = oldList[r];
            oldList.RemoveAt(r);
        }
    }

    /// <summary>
    /// 移除一个列表中所有另一个列表的元素
    /// 返回一个全新的列表，不保证原有顺序
    /// </summary>
    /// <param name="sourceList"></param>
    /// <param name="removeList"></param>
    /// <returns></returns>
    public static List<int> GetRandomAfterRemoval(this List<int> sourceList, List<int> removeList)
    {
        List<int> filteredList = sourceList.Except(removeList).ToList();
        return filteredList;
    }
    

    /// <summary>
    /// 移除一个列表中所有另一个列表的元素
    /// 返回一个全新的列表，不保证原有顺序
    /// </summary>
    /// <param name="sourceList"></param>
    /// <param name="removeList"></param>
    /// <returns></returns>
    public static List<long> GetRandomAfterRemoval(this List<long> sourceList, List<long> removeList)
    {
        List<long> filteredList = sourceList.Except(removeList).ToList();
        return filteredList;
    }
    
    /// <summary>
    /// 移除一个列表中所有另一个列表的元素
    /// 返回一个全新的列表，保证原有顺序
    /// </summary>
    /// <param name="sourceList"></param>
    /// <param name="removeList"></param>
    /// <returns></returns>
    public static List<int> GetLineAfterRemoval(this List<int> sourceList, List<int> removeList)
    {
        if (removeList == null || removeList.Count == 0) return new List<int>(sourceList);
        var secondSet = new HashSet<int>(removeList);
        var whereResult = sourceList.Where(item => !secondSet.Contains(item)).ToList();
        return whereResult;
    }
    
    
    /// <summary>
    /// 移除一个列表中所有另一个列表的元素
    /// 返回一个全新的列表，保证原有顺序
    /// </summary>
    /// <param name="sourceList"></param>
    /// <param name="removeList"></param>
    /// <returns></returns>
    public static List<ulong> GetLineAfterRemoval(this List<ulong> sourceList, List<ulong> removeList)
    {
        if (removeList == null || removeList.Count == 0) return new List<ulong>(sourceList);
        var secondSet = new HashSet<ulong>(removeList);
        var whereResult = sourceList.Where(item => !secondSet.Contains(item)).ToList();
        return whereResult;
    }
    
    
    /// <summary>
    /// 移除一个列表中所有另一个列表的元素
    /// 返回一个全新的列表，保证原有顺序
    /// </summary>
    /// <param name="sourceList"></param>
    /// <param name="removeList"></param>
    /// <returns></returns>
    public static List<long> GetLineAfterRemoval(this List<long> sourceList, List<ulong> removeList)
    {
        if (removeList == null || removeList.Count == 0) return new List<long>(sourceList);
        var secondSet = new HashSet<ulong>(removeList);
        var whereResult = sourceList.Where(item => !secondSet.Contains((ulong)item)).ToList();
        return whereResult;
    }
    
    /// <summary>
    /// 移除一个列表中所有另一个列表的元素
    /// 返回一个全新的列表，保证原有顺序
    /// </summary>
    /// <param name="sourceList"></param>
    /// <param name="removeList"></param>
    /// <returns></returns>
    public static List<long> GetLineAfterRemoval(this List<long> sourceList, List<long> removeList)
    {
        if (removeList == null || removeList.Count == 0) return new List<long>(sourceList);
        var secondSet = new HashSet<long>(removeList);
        var whereResult = sourceList.Where(item => !secondSet.Contains(item)).ToList();
        return whereResult;
    }

    /// <summary>
    /// 打印出一个数组列表
    /// </summary>
    /// <param name="list"></param>
    public static void Printf(this List<int> list)
    {
        if (list == null) return;
        int count = list.Count;
        string str = "";
        for (int i = 0; i < count; i++)
        {
            str = str + list[i] + "\t";
        }
        TEngine.Log.Info(str);
    }

    /// <summary>
    /// 打印出一个数组列表
    /// </summary>
    /// <param name="list"></param>
    public static void Printf(this List<string> list)
    {
        if (list == null) return;
        int count = list.Count;
        string str = "";
        for (int i = 0; i < count; i++)
        {
            str = str + list[i] + "\t";
        }
        TEngine.Log.Info(str);
    }



    #endregion

    #region Other

    /// <summary>
    /// String conversion color
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public static Color GetColor(this string color)
    {
        ColorUtility.TryParseHtmlString(color, out Color colorGold);
        return colorGold;
    }

    /// <summary>
    /// 反序列化
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <returns></returns>
    public static T FromJson<T>(this string self)
    {
        return JsonConvert.DeserializeObject<T>(self, new JsonSerializerSettings()
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace
        });
    }
    /// <summary>
    /// 转化成JSON
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self">对象</param>
    /// <param name="prettyPrint">是否格式化</param>
    /// <returns></returns>
    public static string ToJson<T>(this T self, bool prettyPrint = false)
    {
        return JsonConvert.SerializeObject(self, prettyPrint ? Formatting.Indented : Formatting.None);
    }

    /// <summary>
    /// 获取一个文件的MD5
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static string GetFileMD5(string filePath)
    {
        using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
        {
            using (System.IO.FileStream stream = System.IO.File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return System.BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }

    #endregion
    
    public static void UpdateTextIntViewTween(Text text, int value, int all, System.Func<int, string> func)
    {
        int valueOld = all - value;
        int valueNew = all;
        
        Sequence updateTextTween = null;
        updateTextTween = DOTween.Sequence();
        updateTextTween.AppendInterval(0.8f);
        updateTextTween.Append(DOTween.To(() => valueOld, x => text.text = func(x), valueNew, 0.7f));
        updateTextTween.AppendCallback(() => { text.text = func(valueNew); });
    }

}

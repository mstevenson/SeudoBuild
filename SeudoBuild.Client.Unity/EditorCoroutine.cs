// using UnityEngine;
// using UnityEditor;
// using System.Collections;
// using System.Collections.Generic;
//
// namespace SeudoBuild.Client.Unity
// {
//     public class EditorCoroutine
//     {
//         class CoroutineRef
//         {
//             //IEnumerator enumerator;
//
//             Stack<IEnumerator> enumStack = new Stack<IEnumerator>();
//             IEnumerator activeEnum;
//
//             bool isComplete;
//
//             public CoroutineRef(IEnumerator enumerator)
//             {
//                 //this.enumerator = enumerator;
//                 enumStack.Push(enumerator);
//             }
//
//             /// <summary>
//             /// Advance the coroutine. Returns true if the coroutine is complete.
//             /// </summary>
//             public bool Tick()
//             {
//                 if (isComplete)
//                 {
//                     return true;
//                 }
//                 if (activeEnum == null)
//                 {
//                     activeEnum = enumStack.Pop();
//                 }
//                 bool stepComplete = activeEnum.MoveNext();
//                 if (stepComplete)
//                 {
//                     activeEnum = null;
//                 }
//                 else
//                 {
//                     if (activeEnum.Current is IEnumerator)
//                     {
//                         enumStack.Push(activeEnum);
//                         activeEnum = (IEnumerator)activeEnum.Current;
//                     }
//                     else if (activeEnum.Current is Coroutine)
//                     {
//                         throw new System.NotSupportedException("EditorCoroutine can not be used with an IEnumerator that calls StartCoroutine inside itself.");
//                     }
//                 }
//                 if (activeEnum == null && enumStack.Count == 0)
//                 {
//                     isComplete = true;
//                     return true;
//                 }
//                 return false;
//             }
//         }
//
//         static List<CoroutineRef> coroutines = new List<CoroutineRef>();
//         static List<CoroutineRef> toRemove = new List<CoroutineRef>();
//
//         [InitializeOnLoadMethod]
//         static void Initialize()
//         {
//             EditorApplication.update += Update;
//         }
//
//         static void Update()
//         {
//             // Pump coroutines
//             foreach (var c in coroutines)
//             {
//                 bool complete = c.Tick();
//                 if (complete)
//                 {
//                     toRemove.Add(c);
//                 }
//             }
//
//             // Clean up
//             foreach (var c in toRemove)
//             {
//                 coroutines.Remove(c);
//             }
//             toRemove.Clear();
//         }
//
//         public static void Start(IEnumerator coroutine)
//         {
//             coroutines.Add(new CoroutineRef(coroutine));
//         }
//     }
// }

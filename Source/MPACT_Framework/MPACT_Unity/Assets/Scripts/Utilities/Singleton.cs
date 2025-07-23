using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    /// <summary>
    /// There can be only one instance of this object and it is globally accessible by other classes.
    /// Classes that inherit the Singleton class will become singleton instances.
    /// Source for this code: https://www.unitygeek.com/unity_c_singleton/
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        private static T instance;

        [Header("Singleton")]
        [SerializeField]
        [Tooltip("When persistent, the object won't be destroyed on scene deload")]
        private bool m_persistent = false;

        public static T Instance {
            get {
                if (instance == null) {
                    instance = FindObjectOfType<T> ();
                    if (instance == null) {
                        GameObject obj = new GameObject ();
                        obj.name = typeof(T).Name;
                        instance = obj.AddComponent<T>();
                    }
                }
                return instance;
            }
        }
        
        public virtual void Awake ()
        {
            // There's no other instance of this singleton
            if (instance == null) {

                instance = this as T;
                if(m_persistent)
                {
                    // Ensures that this object won't be destroyed when the scene changes
                    DontDestroyOnLoad (this.gameObject);
                }
            } else {

                // There's another singleton that was instanced first
                if( instance != this ) 
                {
                    Destroy (gameObject);
                }
            }
        }
    }


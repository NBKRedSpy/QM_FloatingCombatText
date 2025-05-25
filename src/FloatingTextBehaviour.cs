using TMPro;
using UnityEngine;

namespace FloatingCombatText
{
    public class FloatingTextBehaviour : MonoBehaviour
    { 
        public TextMeshPro TextComponent { get; set; }
        public float RemainingTime { get; set; } = 3;
        public float FloatSpeed { get; set; } = 0.1f;

        void Start()
        {
            TextComponent = GetComponentInParent<TextMeshPro>();
        }


        void Update()
        {
            RemainingTime -= Time.deltaTime;
            if (RemainingTime < 0)
            {
                Destroy(TextComponent);
                Destroy(transform.parent);
                Destroy(this);
                return;
            }

            TextComponent.transform.localPosition += new Vector3(0, FloatSpeed * Time.deltaTime, 0);
        }
    }
}

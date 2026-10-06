using UnityEngine;

namespace Archive0317
{
    public abstract class Inspectable : MonoBehaviour
    {
        public virtual string Prompt => "E 조사";
        public abstract void Inspect(FirstPersonPlayer player);
    }
}

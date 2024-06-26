using System;
using UnityEngine.UI;

namespace ConstellationUI
{
    public class UILineChunk : MaskableGraphic
    {
        private Action<VertexHelper> _onPopulateMeshAction;
        public Action<VertexHelper> OnPopulateMeshAction
        {
            get => _onPopulateMeshAction;
            set
            {
                _onPopulateMeshAction = value;
                UpdateGeometry();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh) => OnPopulateMeshAction(vh);

        protected override void Awake()
        {
            base.Awake();
            useLegacyMeshGeneration = false;
        }
    }
}

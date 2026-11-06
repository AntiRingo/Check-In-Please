using UnityEngine;

namespace CheckInPlease
{
    // Blend the complete scene into a second view containing only the keeper and connected
    // stairs. This fades walls, signs and residents together without transparency sorting or
    // making unseen rooms visible. UI is drawn afterwards and stays legible.
    [RequireComponent(typeof(Camera))]
    public sealed class StoreyTransitionEffect : MonoBehaviour
    {
        public const int PlayerLayer=27,StoreyLayer=28,StairwellLayer=29;
        WorldView world;
        Camera sceneCamera,stairCamera;
        Material blend;
        public void Setup(WorldView owner)
        {
            world=owner;sceneCamera=GetComponent<Camera>();
            blend=new Material(Resources.Load<Shader>("HotelStoreyBlend")){hideFlags=HideFlags.HideAndDontSave};
            var helper=new GameObject("Persistent stairwell render"){hideFlags=HideFlags.HideAndDontSave};
            helper.transform.SetParent(transform,false);stairCamera=helper.AddComponent<Camera>();stairCamera.enabled=false;
        }
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            float opacity=world==null?1:world.FloorOpacity;
            if(opacity>=.999f||blend==null){Graphics.Blit(source,destination);return;}
            var descriptor=source.descriptor;descriptor.depthBufferBits=24;descriptor.msaaSamples=1;
            var stairs=RenderTexture.GetTemporary(descriptor);
            try
            {
                stairCamera.CopyFrom(sceneCamera);stairCamera.enabled=false;
                stairCamera.transform.SetPositionAndRotation(sceneCamera.transform.position,sceneCamera.transform.rotation);
                stairCamera.rect=new Rect(0,0,1,1);stairCamera.aspect=sceneCamera.aspect;
                stairCamera.cullingMask=(sceneCamera.cullingMask|(1<<StairwellLayer))&~((1<<StoreyLayer)|(1<<PlayerLayer));
                stairCamera.targetTexture=stairs;stairCamera.Render();
                // Keep the keeper readable behind the rising return flight in the isolated
                // stairwell. Only its intermediate view uses this pass; normal floor depth stays intact.
                stairCamera.clearFlags=CameraClearFlags.Depth;stairCamera.cullingMask=1<<PlayerLayer;stairCamera.Render();
                blend.SetTexture("_StairTex",stairs);blend.SetFloat("_FloorOpacity",opacity);
                Graphics.Blit(source,destination,blend);
            }
            finally{stairCamera.targetTexture=null;RenderTexture.ReleaseTemporary(stairs);}
        }
        void OnDestroy()
        {
            if(stairCamera!=null)Destroy(stairCamera.gameObject);
            if(blend!=null)Destroy(blend);
        }
    }
}

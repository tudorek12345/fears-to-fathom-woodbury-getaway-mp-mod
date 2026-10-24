using System;
using UnityEngine;
using WoodburySpectatorSync.Coop;

namespace WoodburySpectatorSync.UI
{
    internal sealed class CharacterPreview : IDisposable
    {
        private GameObject _root;
        private TravelerVisual _model;
        private Camera _camera;
        private RenderTexture _texture;
        private bool _dirty=true, _face;
        private float _yaw;
        private int _renderAfterFrame;
        public Texture Texture => _texture;
        public void Initialize()
        {
            if(_root!=null) return;
            if(_texture!=null) Dispose(); // Scene unload destroyed the studio, but not its native texture.
            _root=new GameObject("Woodbury character studio"); _root.transform.position=new Vector3(1000,1000,1000);
            _model=TravelerVisual.Create(_root.transform,CharacterProfile.Default);
            foreach(var part in _model.GetComponentsInChildren<Transform>(true)) part.gameObject.layer=31;
            var camera=new GameObject("Character preview camera"); camera.transform.SetParent(_root.transform,false);
            _camera=camera.AddComponent<Camera>(); _camera.enabled=false; _camera.cullingMask=1<<31;
            _camera.clearFlags=CameraClearFlags.SolidColor; _camera.backgroundColor=new Color(.065f,.085f,.10f);
            _camera.orthographic=true; _camera.nearClipPlane=.1f; _camera.farClipPlane=8;
            _texture=new RenderTexture(512,640,16); _texture.name="Character preview";
            if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) _texture.Create();
            _camera.targetTexture=_texture;
            Light("Key",new Vector3(-1.8f,2.8f,2.2f),2.7f,new Color(1,.89f,.76f));
            Light("Fill",new Vector3(1.8f,1.8f,1.5f),1.6f,new Color(.66f,.8f,1));
            Light("Rim",new Vector3(0,2.4f,-1.4f),2.1f,new Color(.8f,.92f,1));
            Frame();
        }
        private void Light(string name,Vector3 position,float intensity,Color color)
        {
            var obj=new GameObject(name); obj.transform.SetParent(_root.transform,false); obj.transform.localPosition=position;
            var light=obj.AddComponent<Light>(); light.type=LightType.Point; light.range=6; light.intensity=intensity; light.color=color; light.cullingMask=1<<31;
        }
        private void Frame()
        {
            if(_camera==null) return;
            _camera.orthographicSize=_face?.29f:1.02f;
            _camera.transform.localPosition=new Vector3(0,_face?1.69f:.94f,3.5f);
            _camera.transform.localRotation=Quaternion.Euler(0,180,0); _dirty=true; _renderAfterFrame=Time.frameCount+2;
        }
        public void Rotate(float delta) { _yaw=(_yaw+delta)%360; _dirty=true; }
        public void ToggleFace() { _face=!_face; Frame(); }
        public void Update(CharacterProfile profile,bool visible)
        {
            if(!visible) { if(_root!=null) _root.SetActive(false); return; }
            Initialize(); if(!_root.activeSelf) { _root.SetActive(true); _dirty=true; }
            if(!_model.Profile.Equals(profile)) { _model.Apply(profile); _dirty=true; _renderAfterFrame=Time.frameCount+2; }
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            if(!_texture.IsCreated()) { _texture.Create(); _dirty=true; }
            // Camera.Render in Update runs before Unity's skinning pass. Let a full
            // frame prepare a changed mesh before taking the on-demand portrait.
            if(!_dirty || Time.frameCount<_renderAfterFrame) return;
            _model.transform.localRotation=Quaternion.Euler(0,_yaw,0); _camera.Render(); _dirty=false;
        }
        public void Dispose()
        {
            if(_camera!=null) _camera.targetTexture=null;
            if(_texture!=null) { _texture.Release(); UnityEngine.Object.Destroy(_texture); }
            if(_root!=null) UnityEngine.Object.Destroy(_root);
            _root=null; _model=null; _camera=null; _texture=null;
        }
    }
}

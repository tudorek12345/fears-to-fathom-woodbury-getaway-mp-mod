using System;
using UnityEngine;
using UnityEngine.UI;

namespace WoodburySpectatorSync.UI
{
    internal static class ModernMenuUI
    {
        public static readonly Color Accent=new Color(.89f,.53f,.28f);
        public static readonly Color Ink=new Color(.055f,.068f,.078f);
        public static readonly Color Card=new Color(.085f,.105f,.12f);
        public static readonly Color Muted=new Color(.58f,.65f,.68f);
        private static Font _font;
        public static Font Font => _font!=null?_font:(_font=UnityEngine.Font.CreateDynamicFontFromOSFont(new[]{"Segoe UI","Arial"},32));
        public static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
        {
            var obj=new GameObject(name,typeof(RectTransform)); obj.transform.SetParent(parent,false);
            var r=(RectTransform)obj.transform; r.anchorMin=new Vector2(x,y); r.anchorMax=new Vector2(x+width,y+height); r.offsetMin=r.offsetMax=Vector2.zero; return r;
        }
        public static RectTransform Panel(Transform parent,string name,float x,float y,float width,float height,Color color)
        { var r=Rect(parent,name,x,y,width,height); var image=r.gameObject.AddComponent<Image>(); image.color=color; return r; }
        public static Text Text(Transform parent,string value,float x,float y,float w,float h,int size=24,Color? color=null)
        {
            var r=Rect(parent,value,x,y,w,h); var text=r.gameObject.AddComponent<Text>(); text.font=Font; text.text=value;
            text.color=color??new Color(.92f,.94f,.93f); text.fontSize=size; text.resizeTextForBestFit=true;
            text.resizeTextMinSize=Math.Max(12,(int)(size*.7f)); text.resizeTextMaxSize=size; text.alignment=TextAnchor.MiddleLeft;
            text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Truncate; text.raycastTarget=false; return text;
        }
        public static Button Button(Transform parent,string label,float x,float y,float w,float h,Action click,bool primary=false)
        {
            var r=Panel(parent,label,x,y,w,h,primary?Accent:new Color(.14f,.17f,.19f));
            var text=Text(r,label,.07f,.02f,.86f,.96f,24,primary?Ink:(Color?)null); text.alignment=TextAnchor.MiddleCenter;
            var button=r.gameObject.AddComponent<Button>(); button.targetGraphic=r.GetComponent<Image>();
            var colors=button.colors; colors.normalColor=Color.white; colors.highlightedColor=new Color(1.2f,1.2f,1.2f); colors.selectedColor=colors.highlightedColor;
            colors.pressedColor=new Color(.8f,.8f,.8f); colors.disabledColor=new Color(.5f,.5f,.5f,.65f); button.colors=colors;
            button.navigation=new Navigation{mode=Navigation.Mode.Automatic}; button.onClick.AddListener(()=>click()); return button;
        }
        public static InputField Field(Transform parent,string label,float x,float y,float w,string value,int limit)
        {
            var row=Rect(parent,label,x,y,w,.15f); Text(row,label,0,.64f,1,.3f,21,Muted);
            var box=Panel(row,"Input",0,0,1,.58f,new Color(.035f,.048f,.06f));
            var text=Text(box,"",.045f,.05f,.91f,.9f,26);
            var input=box.gameObject.AddComponent<InputField>(); input.textComponent=text; input.targetGraphic=box.GetComponent<Image>();
            input.characterLimit=limit; input.lineType=InputField.LineType.SingleLine; input.text=value;
            var colors=input.colors; colors.selectedColor=new Color(1.5f,1.5f,1.5f); input.colors=colors; return input;
        }
    }
}

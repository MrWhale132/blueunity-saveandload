
using System.Collections.Generic;
using Theblueway.TypeBinding.Editor.TypeInterfaceScripts;

namespace Theblueway.SaveAndLoad.Runtime.Packages.com.blueutils.saveandload.Runtime.TypeInterfaces
{
    [TypeInterface(374880314333943428, typeof(UnityEngine.Texture))]
    public class TextureTypeInterface:TypeInterface<UnityEngine.Texture>
    {
        static TypeMember[] _members = new TypeMember[]
                {
                    (0,imageContentsHash),
                };
        public override IEnumerable<TypeMember> GetMembers()
        {
            return _members;
        }
        static string imageContentsHash => nameof(UnityEngine.Texture.imageContentsHash);
    }
}

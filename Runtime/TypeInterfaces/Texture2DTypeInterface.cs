
using System.Collections.Generic;
using Theblueway.TypeBinding.Runtime.TypeInterfaceScripts;

namespace Theblueway.SaveAndLoad.Runtime.Packages.com.blueutils.saveandload.Runtime.TypeInterfaces
{
    [TypeInterface(245129840311300530, typeof(UnityEngine.Texture2D))]
    public class Texture2DTypeInterface:TypeInterface<UnityEngine.Texture2D>
    {
        static TypeMember[] _members = new TypeMember[]
                {
                     (0,alphaIsTransparency),
                };
        public override IEnumerable<TypeMember> GetMembers()
        {
            return _members;
        }
        static string alphaIsTransparency => nameof(UnityEngine.Texture2D.alphaIsTransparency);
    }
}

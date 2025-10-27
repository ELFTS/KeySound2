using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace KeySound2.Properties;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
[CompilerGenerated]
internal class Resources
{
    private static ResourceManager resourceMan;
    private static CultureInfo resourceCulture;
    
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static ResourceManager ResourceManager
    {
        get
        {
            if (resourceMan == null)
            {
                ResourceManager temp = new ResourceManager("KeySound2.Properties.Resources", typeof(Resources).Assembly);
                resourceMan = temp;
            }
            return resourceMan;
        }
    }
    
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static CultureInfo Culture
    {
        get { return resourceCulture; }
        set { resourceCulture = value; }
    }
    
    /// <summary>
    /// 查找与指定的名称关联的资源。
    /// </summary>
    internal static byte[] key_sound
    {
        get { return (byte[])ResourceManager.GetObject("key_sound", resourceCulture); }
    }
}
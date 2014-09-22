using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

using System.IO;

namespace HandlebarsViewEngine
{
    // Development versions:
    // -> Local file system, doesn't use git version
    // -> Raw template from blob storage (for in page editor)
    //    doesn't need version number
    
    // Production versions:
    // -> Download precompiled JS for GIT version 
    // -> Domain specific GIT versions of precompiled content


    // 
    // Multiple versions:
    // 1. Version will reads from the local file system (for development proxy)
    // 2. Another version which loads the pre-compiled handlebars templates from 
    //    

    // When changing versions check the version is valid before changing.

}

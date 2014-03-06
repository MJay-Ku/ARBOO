// Glyph Recognition Studio
// http://www.aforgenet.com/projects/gratf/
//
// Copyright © Andrew Kirillov, 2010-2011
// andrew.kirillov@aforgenet.com
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Control
{
    // Enumeration of visualization types
    enum VisualizationType
    {
        // Hightlight glyph with border only
        BorderOnly,
        // Hightlight glyph with border and put its name in the center
        Name,
        // Substitue glyph with its image
        Image,
        //Audio by Tag Tech
        Audio,
        // Show 3D model over the glyph
        Model
    }
}

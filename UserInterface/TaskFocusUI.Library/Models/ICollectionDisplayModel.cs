using System;
using System.Collections.Generic;
using System.Text;

namespace TaskFocusUI.Library.Models
{
    public interface ICollectionDisplayModel
    {
        int? Id { get; set; }
        int? OrderIndex { get; set; }
    }
}

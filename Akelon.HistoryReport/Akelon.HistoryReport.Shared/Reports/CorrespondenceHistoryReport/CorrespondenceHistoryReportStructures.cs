using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Akelon.HistoryReport.Structures.CorrespondenceHistoryReport
{

  /// <summary>
  /// Строчка отчета.
  /// </summary>
  partial class TableLine
  {
    public string ReportSessionId { get; set; }
    
    public string Id { get; set; }

    public string ParentId { get; set; }

    public string Subject { get; set; }
    
    public string TaskHyperlink { get; set; }
    
    public string ParentTaskHyperlink { get; set; }
    
    public string EmployeeName { get; set; }
    
    public string ActiveText { get; set; }
    
    public string Performers { get; set; }
    
    public string Observers { get; set; }

    public string Created { get; set; }

    public string Deadline { get; set; }

    public string Status { get; set; }
    
    public string TaskId { get; set; }
    
    public string ParentTaskId { get; set; }
  }

}
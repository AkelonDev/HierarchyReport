using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace Akelon.HistoryReport.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      CreateTablesForReports();
    }
    
    /// <summary>
    /// Создать таблицы для отчетов.
    /// </summary>
    public void CreateTablesForReports()
    {
      // Отчет "История переписки";
      var correspondenceHistoryReportTableName = Constants.CorrespondenceHistoryReport.SourceTableName;
      
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTables(new[] { correspondenceHistoryReportTableName });
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.CorrespondenceHistoryReport.CreateSourceTable, new[] { correspondenceHistoryReportTableName });
    }
  }
}

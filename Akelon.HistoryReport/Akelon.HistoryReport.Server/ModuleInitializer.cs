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
    /// Создание таблиц для отчетов.
    /// </summary>
    public void CreateTablesForReports()
    {
      // Создание таблицы для отчета "История переписки".
      var hierarchyReportTableName = Constants.HierarchyReport.SourceTableName;
      
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTables(new[] { hierarchyReportTableName });
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.HierarchyReport.CreateSourceTable, new[] { hierarchyReportTableName });
    }
  }
}

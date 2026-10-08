using System;
using Sungero.Core;

namespace Akelon.HistoryReport.Constants
{
  public static class HierarchyReport
  {

    /// <summary>
    /// Имя временной таблицы для отчета "История переписки".
    /// </summary>
    public const string SourceTableName = "Akelon_Reports_HierarchyReport";
    
    /// <summary>
    /// Разделители.
    /// </summary>
    [Public]
    public static class Delimeters
    {
      /// <summary>
      /// Точка с запятой.
      /// </summary>
      [Public]
      public const string Semicolon = "; ";
    }

  }
}
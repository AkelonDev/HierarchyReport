using System;
using Sungero.Core;

namespace Akelon.HistoryReport.Constants
{
  public static class CorrespondenceHistoryReport
  {

    /// <summary>
    /// Имя временной таблицы.
    /// </summary>
    public const string SourceTableName = "Akelon_Reports_CorrespondenceHistory";
    
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
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Akelon.HistoryReport
{
  partial class CorrespondenceHistoryReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.CorrespondenceHistoryReport.SourceTableName, CorrespondenceHistoryReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var reportSessionId = System.Guid.NewGuid().ToString();
      var tableData = new List<Structures.CorrespondenceHistoryReport.TableLine>();
      CorrespondenceHistoryReport.ReportSessionId = reportSessionId;
      var task = CorrespondenceHistoryReport.Task;
      CorrespondenceHistoryReport.Subject = task.Subject;
      CorrespondenceHistoryReport.Level = 1;
      var assignments = Sungero.Workflow.Assignments.GetAll(x => Equals(x.Task, task));
      
      // Задача с разделением по StartId.
      for (var startId = 1; startId <= task.StartId; startId++)
      {
        var row = Structures.CorrespondenceHistoryReport.TableLine.Create();
        var historyDates = task.History.GetAll().Where(x => x.Operation == Sungero.Workflow.WorkflowHistory.Operation.Start).OrderBy(x => x.HistoryDate).ToList();
        var author = task.Author;
        
        row.ActiveText = task.ActiveText;
        row.Performers = Akelon.HistoryReport.Reports.Resources.CorrespondenceHistoryReport.ToFormat(string.Join(Constants.CorrespondenceHistoryReport.Delimeters.Semicolon, assignments.Where(x => x.TaskStartId == startId).Select(x => x.Performer.Name)));
        row.Observers = Akelon.HistoryReport.Reports.Resources.CorrespondenceHistoryReport.ObserversFormat(string.Join(Constants.CorrespondenceHistoryReport.Delimeters.Semicolon, task.Observers.Select(x => x.Observer.Name)));
        row.EmployeeName = Sungero.Company.Employees.Is(author) ? Akelon.HistoryReport.Reports.Resources.CorrespondenceHistoryReport.EmployeeNameFormat(author.Name, Sungero.Company.Employees.As(author).Department) : author.Name;
        row.Subject = task.Subject;
        row.TaskHyperlink = Hyperlinks.Get(task);
        row.Created = historyDates[startId - 1].HistoryDate.Value.ToString("g");
        row.TaskId = Akelon.HistoryReport.Reports.Resources.CorrespondenceHistoryReport.TaskIdFormat(task.Id);
        row.Id = "-1";
        
        tableData.Add(row);
        
        // Подзадачи задачи.
        FillSubTasks(task.Subtasks.Where(x => x.ParentStartId == startId), tableData, null);
      }
      
      // Задания.
      FillAssignments(assignments, tableData, null);
      
      foreach (var row in tableData)
        row.ReportSessionId = reportSessionId;
      
      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.CorrespondenceHistoryReport.SourceTableName, tableData);
    }
    
    /// <summary>
    /// Заполнить таблицу заданиями.
    /// </summary>
    /// <param name="assignments">Задания.</param>
    /// <param name="tableData">Таблица.</param>
    /// <param name="parentId">ИД родительской задачи.</param>
    public void FillAssignments(IQueryable<Sungero.Workflow.IAssignment> assignments, List<Structures.CorrespondenceHistoryReport.TableLine> tableData, string parentId)
    {
      foreach (var assignment in assignments)
      {
        var row = Structures.CorrespondenceHistoryReport.TableLine.Create();
        var performer = Sungero.Company.Employees.As(assignment.Performer);
        var assignmentId = assignment.Id.ToString();
        
        row.ActiveText = assignment.ActiveText;
        row.Created = assignment.Modified.Value.ToString("g");
        row.Deadline = assignment.Deadline != null ? Akelon.HistoryReport.Reports.Resources.CorrespondenceHistoryReport.DeadlineFormat(assignment.Deadline.Value.ToString("g")) : string.Empty;
        row.EmployeeName = Akelon.HistoryReport.Reports.Resources.CorrespondenceHistoryReport.EmployeeNameFormat(performer.Name, performer.Department);
        row.Id = assignmentId;
        row.ParentId = parentId;
        row.Status = Sungero.Workflow.Assignments.Info.Properties.Status.GetLocalizedValue(assignment.Status);
        
        tableData.Add(row);
        
        // Подзадачи. Макс. уровень иерархии - 10.
        if (CorrespondenceHistoryReport.Level < 11)
          FillSubTasks(assignment.Subtasks, tableData, assignmentId);
        else
          FillSubTasks(assignment.Subtasks, tableData, parentId);
      }
    }
    
    /// <summary>
    /// Заполнить таблицу подзадачами.
    /// </summary>
    /// <param name="subTasks">Подзадачи.</param>
    /// <param name="tableData">Таблица.</param>
    /// <param name="parentId">ИД родительской задачи.</param>
    public void FillSubTasks(IEnumerable<Sungero.Workflow.ITask> subTasks, List<Structures.CorrespondenceHistoryReport.TableLine> tableData, string parentId)
    {
      if (!subTasks.Any())
        return;
      
      CorrespondenceHistoryReport.Level++;
    
      foreach (var subTask in subTasks)
      {
        // Подзадачи с разделением по StartId.
        for (var startId = 1; startId <= subTask.StartId; startId++)
        {
          var subTaskText = subTask.Texts.Where(x => x.StartId == startId).FirstOrDefault();
          var row = Structures.CorrespondenceHistoryReport.TableLine.Create();
          var subAssignments = Sungero.Workflow.Assignments.GetAll(x => Equals(x.Task, subTask) && x.TaskStartId == startId).OrderBy(x => x.Created);
          var performers = subAssignments.Select(x => x.Performer.Name);
          var author = subTaskText.Author;
          var parentTask = Sungero.Workflow.Tasks.Null;
          
          row.ActiveText = subTaskText.Body;
          row.Performers = Akelon.HistoryReport.Reports.Resources.CorrespondenceHistoryReport.ToFormat(string.Join(Constants.CorrespondenceHistoryReport.Delimeters.Semicolon, performers));
          row.Observers = Akelon.HistoryReport.Reports.Resources.CorrespondenceHistoryReport.ObserversFormat(string.Join(Constants.CorrespondenceHistoryReport.Delimeters.Semicolon, subTask.Observers.Select(x => x.Observer.Name)));
          row.EmployeeName = Sungero.Company.Employees.Is(author) ? Akelon.HistoryReport.Reports.Resources.CorrespondenceHistoryReport.EmployeeNameFormat(author.Name, Sungero.Company.Employees.As(author).Department) : author.Name;
          row.Subject = Akelon.HistoryReport.Reports.Resources.CorrespondenceHistoryReport.SubtaskText;
          row.TaskHyperlink = Hyperlinks.Get(subTask);
          row.Created = subTaskText.Modified.Value.ToString("g");
          row.Deadline = subTask.MaxDeadline != null ? Akelon.HistoryReport.Reports.Resources.CorrespondenceHistoryReport.DeadlineFormat(subTask.MaxDeadline.Value.ToString("g")) : string.Empty;
          row.Id = string.Format("{0}_{1}", subTask.Id, startId);
          row.TaskId = Akelon.HistoryReport.Reports.Resources.CorrespondenceHistoryReport.TaskIdFormat(subTask.Id);
          
          if (!string.IsNullOrEmpty(parentId))
          {
            parentTask = subTask.ParentAssignment.Task;
            row.ParentId = parentId;
          }
          else
          {
            parentTask = subTask.ParentTask;
          }
          
          row.ParentTaskId = Akelon.HistoryReport.Reports.Resources.CorrespondenceHistoryReport.ParentTaskIdFormat(parentTask.Id);
          row.ParentTaskHyperlink = Hyperlinks.Get(parentTask);
          
          tableData.Add(row);
          
          // Задания подзадачи.
          FillAssignments(subAssignments, tableData, row.ParentId);
        }
      }
    }

  }
}
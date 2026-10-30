using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Akelon.HistoryReport
{
  partial class HierarchyReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.HierarchyReport.SourceTableName, HierarchyReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      // Формирование отчета является ресурсоемкой операцией из-за использования вложенных циклов при обработке запусков задач, подзадач и их заданий.
      // При большом объеме данных или увеличении максимального уровня иерархии - время его формирования может возрастать.
      var reportSessionId = System.Guid.NewGuid().ToString();
      var tableData = new List<Structures.HierarchyReport.TableLine>();
      HierarchyReport.ReportSessionId = reportSessionId;
      var task = HierarchyReport.Task;
      HierarchyReport.Subject = task.Subject;
      HierarchyReport.Level = 1;
      var assignments = Sungero.Workflow.Assignments.GetAll(x => Equals(x.Task, task));
      
      // Получение общих данных по главной задаче, вне зависимости от итерации запуска.
      var author = Sungero.Company.Employees.Is(task.Author) ?
        Akelon.HistoryReport.Reports.Resources.HierarchyReport.EmployeeNameFormat(task.Author.Name, Sungero.Company.Employees.As(task.Author).Department) : task.Author.Name;
      var observers = Akelon.HistoryReport.Reports.Resources.HierarchyReport.ObserversFormat(string.Join(Constants.HierarchyReport.Delimeters.Semicolon, task.Observers.Select(x => x.Observer.Name)));
      var historyDates = task.History.GetAll().Where(x => x.Operation == Sungero.Workflow.WorkflowHistory.Operation.Start).OrderBy(x => x.HistoryDate).ToList();
      var activeText = task.ActiveText;
      var subject = task.Subject;
      var taskId = Akelon.HistoryReport.Reports.Resources.HierarchyReport.TaskIdFormat(task.Id);
      var taskHyperlink = Hyperlinks.Get(task);
      
      // Заполнение информации по каждому запуску задачи, итерации запусков определяются по StartId.
      for (var startId = 1; startId <= task.StartId; startId++)
      {
        var row = Structures.HierarchyReport.TableLine.Create();
        row.ActiveText = activeText;
        row.Performers = Akelon.HistoryReport.Reports.Resources.HierarchyReport.ToFormat(string.Join(Constants.HierarchyReport.Delimeters.Semicolon, assignments.Where(x => x.TaskStartId == startId).Select(x => x.Performer.Name)));
        row.Observers = observers;
        row.EmployeeName = author;
        row.Subject = subject;
        row.TaskHyperlink = taskHyperlink;
        row.Created = historyDates[startId - 1].HistoryDate.Value.ToString("g");
        row.TaskId = taskId;
        
        // Значение -1 необходимо для обозначения корневого элемента отчета. Это позволяет отображать MainTask в самом верху иерархии.
        row.Id = "-1";
        
        tableData.Add(row);
        
        // Заполнение информации про подзадачи текущей итерации запуска задачи.
        FillSubTasks(task.Subtasks.Where(x => x.ParentStartId == startId), tableData, null);
      }
      
      // Заполнение информации про задания основной задачи.
      FillAssignments(assignments, tableData, null);
      
      // Заполнение ReportSessionId происходит после формирования данных отчета для избежания передачи дополнительной переменной в методы.
      foreach (var row in tableData)
        row.ReportSessionId = reportSessionId;
      
      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.HierarchyReport.SourceTableName, tableData);
    }
    
    /// <summary>
    /// Заполнение таблицы отчета данными про заданиями.
    /// </summary>
    /// <param name="assignments">Задания.</param>
    /// <param name="tableData">Таблица.</param>
    /// <param name="parentId">ИД родительской задачи.</param>
    public void FillAssignments(IQueryable<Sungero.Workflow.IAssignment> assignments, List<Structures.HierarchyReport.TableLine> tableData, string parentId)
    {
      // Сортировка по Дате создания необходима для отображения элементов в том же порядке, как в переписке задачи - т.е. в разрезе блоков иерархии.
      foreach (var assignment in assignments.OrderBy(x => x.Created))
      {
        var row = Structures.HierarchyReport.TableLine.Create();
        var performer = Sungero.Company.Employees.As(assignment.Performer);
        var assignmentId = assignment.Id.ToString();
        
        row.ActiveText = assignment.ActiveText;
        row.Created = assignment.Modified.Value.ToString("g");
        row.Deadline = assignment.Deadline != null ? Akelon.HistoryReport.Reports.Resources.HierarchyReport.DeadlineFormat(assignment.Deadline.Value.ToString("g")) : string.Empty;
        row.EmployeeName = Akelon.HistoryReport.Reports.Resources.HierarchyReport.EmployeeNameFormat(performer.Name, performer.Department);
        row.Status = Sungero.Workflow.Assignments.Info.Properties.Status.GetLocalizedValue(assignment.Status);
        
        // Заполнение идентификатора текущего элемента для построения иерархии отчета.
        row.Id = assignmentId;
        
        // Заполнение идентификатора родительского элемента в иерархии отчета.
        // Для основной задачи и заданий основной задачи parentId равен null, т.к. они находятся на одном уровне иерархии. В иных случаях указывается Id родительского элемента.
        row.ParentId = parentId;
        
        tableData.Add(row);
        
        // Заполнение информации про подзадачи, созданные в рамках текущего задания. Макс. уровень глубины иерархии - 10.
        if (HierarchyReport.Level < 11)
          FillSubTasks(assignment.Subtasks, tableData, assignmentId);
        else
          FillSubTasks(assignment.Subtasks, tableData, parentId);
      }
    }
    
    /// <summary>
    /// Заполнение таблицы отчета данными про подзадачи.
    /// </summary>
    /// <param name="subTasks">Подзадачи.</param>
    /// <param name="tableData">Таблица.</param>
    /// <param name="parentId">ИД родительской задачи.</param>
    public void FillSubTasks(IEnumerable<Sungero.Workflow.ITask> subTasks, List<Structures.HierarchyReport.TableLine> tableData, string parentId)
    {
      if (!subTasks.Any())
        return;
      
      HierarchyReport.Level++;
      
      // Сортировка по Дате создания необходима для отображения элементов в том же порядке, как в переписке задачи - т.е. в разрезе блоков иерархии.
      foreach (var subTask in subTasks.OrderBy(x => x.Created))
      {
        // Получение общих данных по подзадачам, вне зависимости от итерации запуска.
        var observers = Akelon.HistoryReport.Reports.Resources.HierarchyReport.ObserversFormat(string.Join(Constants.HierarchyReport.Delimeters.Semicolon, subTask.Observers.Select(x => x.Observer.Name)));
        var deadline = subTask.MaxDeadline != null ? Akelon.HistoryReport.Reports.Resources.HierarchyReport.DeadlineFormat(subTask.MaxDeadline.Value.ToString("g")) : string.Empty;
        
        // Заполнение информации по каждому запуску подзадачи, итерации запусков определяются по StartId.
        for (var startId = 1; startId <= subTask.StartId; startId++)
        {
          var subTaskText = subTask.Texts.Where(x => x.StartId == startId).FirstOrDefault();
          var row = Structures.HierarchyReport.TableLine.Create();
          var subAssignments = Sungero.Workflow.Assignments.GetAll(x => Equals(x.Task, subTask) && x.TaskStartId == startId).OrderBy(x => x.Created);
          var performers = subAssignments.Select(x => x.Performer.Name);
          var author = subTaskText.Author;
          var parentTask = Sungero.Workflow.Tasks.Null;
          
          row.ActiveText = subTaskText.Body;
          row.Performers = Akelon.HistoryReport.Reports.Resources.HierarchyReport.ToFormat(string.Join(Constants.HierarchyReport.Delimeters.Semicolon, performers));
          row.Observers = observers;
          row.EmployeeName = Sungero.Company.Employees.Is(author) ? Akelon.HistoryReport.Reports.Resources.HierarchyReport.EmployeeNameFormat(author.Name, Sungero.Company.Employees.As(author).Department) : author.Name;
          row.Subject = Akelon.HistoryReport.Reports.Resources.HierarchyReport.SubtaskText;
          row.TaskHyperlink = Hyperlinks.Get(subTask);
          row.Created = subTaskText.Modified.Value.ToString("g");
          row.Deadline = deadline;
          
          // Формирование идентификатора текущей подзадачи с учетом ее запусков.
          // Одна задача может быть запущена несколько раз, поэтому одного Id подзадачи недостаточно для различия строк, соответствующим разным запускам.
          row.Id = string.Format("{0}_{1}", subTask.Id, startId);
          row.TaskId = Akelon.HistoryReport.Reports.Resources.HierarchyReport.TaskIdFormat(subTask.Id);
          
          if (!string.IsNullOrEmpty(parentId))
          {
            // Заполнение информации и Id родительской задачи, если текущая подзадача создана в рамках задания.
            parentTask = subTask.ParentAssignment.Task;
            row.ParentId = parentId;
          }
          else
          {
            // Заполнение информации про родительскую задачу, если текущая подзадача создана в рамках основной задачи.
            parentTask = subTask.ParentTask;
          }
          
          row.ParentTaskId = Akelon.HistoryReport.Reports.Resources.HierarchyReport.ParentTaskIdFormat(parentTask.Id);
          row.ParentTaskHyperlink = Hyperlinks.Get(parentTask);
          
          tableData.Add(row);
          
          // Заполнение информации про задания текущей итерации запуска подзадачи.
          FillAssignments(subAssignments, tableData, row.ParentId);
        }
      }
    }

  }
}
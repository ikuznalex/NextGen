﻿//////////////////////////////////////////////////////////////////
//
// Youbiquitous YBQ : app starter 
// Copyright (c) Youbiquitous srls 2017
//
// Author: Dino Esposito (http://youbiquitous.net)
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Memento;
using Memento.Messaging.Postie;
using Memento.Persistence;
using Mfx2.CommandStack.Commands;
using Mfx2.CommandStack.Model;
using Mfx2.ReadStack.Denormalizers;
using Mfx2.Shared.Events;

namespace Mfx2.CommandStack.Sagas
{
    public class ManageTaskSaga : Saga,
        IAmStartedBy<AddNewTaskCommand>,
        IHandleMessages<UpdateTaskCommand>,
        IHandleMessages<DeleteTaskCommand>,
        IHandleMessages<MarkCompletedTaskCommand>,
        IHandleMessages<RegenerateReadModelCommand>
    {
        public ManageTaskSaga(IBus bus, IEventStore eventStore, IRepository repository)
            : base(bus, eventStore, repository)
        {
        }

        public void Handle(AddNewTaskCommand message)
        {
            var task = Task.Factory.NewTaskFrom(
                message.Title, message.Description, message.DueDate, message.Priority);
            Repository.Save(task);

            // Notify back
            var notification = new AddNewTaskNotifyCommand(message.SignalrConnectionId)
            {
                TaskId = task.TaskId,
                Title = task.Title
            };
            Bus.Send(notification);
        }

        public void Handle(UpdateTaskCommand message)
        {
            // Dehydrates all events from event store for given aggregate
            var task = Repository.GetById<Task>(message.TaskId);

            // Check if there are real changes to apply
            var same = task.IsSameContent(message.Title,
                message.Description,
                message.DueDate,
                message.Priority,
                message.Status);
            if (same)
            {
                var notifyNoChanges = new NoUpdatesNotifyCommand(message.SignalrConnectionId)
                {
                    TaskId = task.TaskId,
                    Title = task.Title
                };
                Bus.Send(notifyNoChanges);
                return;
            }

            // Business validation
            if (!task.CanUpdate(message.Title, message.Description, message.DueDate, message.Priority, message.Status))
            {
                var notifyCantUpdate = new CantUpdateNotifyCommand(message.SignalrConnectionId)
                {
                    TaskId = task.TaskId,
                    Title = task.Title
                };
                Bus.Send(notifyCantUpdate);
                return;
            }

            // Triggers the UPDATE-GENERAL event
            task.UpdateModel(message.Title, message.Description, message.DueDate, message.Priority, message.Status);
            Repository.Save(task);

            // Notify back
            var notification = new UpdateTaskNotifyCommand(message.SignalrConnectionId)
            {
                TaskId = task.TaskId,
                Title = task.Title
            };
            Bus.Send(notification);
        }

        public void Handle(DeleteTaskCommand message)
        {
            var task = Repository.GetById<Task>(message.TaskId);
            task.MarkAsDeleted();
            Repository.Save(task);
        }

        public void Handle(MarkCompletedTaskCommand message)
        {
            var task = Repository.GetById<Task>(message.TaskId);
            task.MarkAsCompleted();
            Repository.Save(task);
        }

        public void Handle(RegenerateReadModelCommand message)
        {
            var denormalizer = new ManageTaskDenormalizer();
            var created = EventStore.Find<TaskCreatedEvent>(theEvent => true).ToList();
            foreach (var e in created)
            {
                denormalizer.Handle(e);
            }
            var updated = EventStore.Find<TaskUpdatedEvent>(theEvent => true).ToList();
            foreach (var e in updated)
            {
                denormalizer.Handle(e);
            }
            var deleted = EventStore.Find<TaskDeletedEvent>(theEvent => true).ToList();
            foreach (var e in deleted)
            {
                denormalizer.Handle(e);
            }
            var completed = EventStore.Find<TaskCompletedEvent>(theEvent => true).ToList();
            foreach (var e in completed)
            {
                denormalizer.Handle(e);
            }
        }
    }
}
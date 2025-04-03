using System;
using System.Collections.Generic;
using System.Text;

namespace Intwenty.Model.Dto
{
    public enum MessageCode
    {
       RESULT = 0
     , USERERROR = 1
     , SYSTEMERROR = 2
     , WARNING = 3
     , INFO = 4
    }

    public class OperationMessage
    {

        public DateTime Date { get; set; }

        public MessageCode Code { get; set; }

        public string Message { get; set; }

        public OperationMessage(MessageCode code, string message)
        {
            Date = DateTime.Now;
            Code = code;
            Message = message;
        }

    }

    public class OperationResult
    {
        public bool IsSuccess { get; set; }
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public List<OperationMessage> Messages { get; set; }

        public OperationResult()
        {
            Messages = new List<OperationMessage>();
        }

        public OperationResult(bool success, MessageCode messagecode = MessageCode.RESULT, string message = "")
        {
            StartTime = DateTime.Now;
            Messages = new List<OperationMessage>();
            IsSuccess = success;
            AddMessage(messagecode, message);
        }

        public bool HasMessage
        {
            get { return Messages.Count > 0; }
        }

        public double Duration
        {
            get { return EndTime.Subtract(StartTime).TotalMilliseconds; }
        }

        public void Finish()
        {
            EndTime = DateTime.Now;
        }

        public void Finish(MessageCode code, string message)
        {
            EndTime = DateTime.Now;
            Messages.Add(new OperationMessage(code, message));
        }

        public void AddMessage(MessageCode code, string message)
        {
            Messages.Add(new OperationMessage(code, message));
        }

        public void SetError(string systemmsg, string usermsg)
        {
            IsSuccess = false;
            Messages.Add(new OperationMessage(MessageCode.SYSTEMERROR, systemmsg));
            Messages.Add(new OperationMessage(MessageCode.USERERROR, usermsg));
        }

        public void SetSuccess(string msg)
        {
            IsSuccess = true;
            Messages.Add(new OperationMessage(MessageCode.RESULT, msg));
        }
    }
}

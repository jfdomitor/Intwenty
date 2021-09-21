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

    public enum LifecycleStatus
    {
        NONE = 0
         , NEW_NOT_SAVED = 1
         , NEW_SAVED = 2
         , EXISTING_NOT_SAVED = 3
         , EXISTING_SAVED = 4
         , DELETED_NOT_SAVED = 5
         , DELETED_SAVED = 6
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


    public class IntwentyResult
    {
        public List<OperationMessage> Messages { get; set; }

        public bool IsSuccess { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public bool HasMessage
        {
            get { return Messages.Count > 0; }
        }

        public bool HasUserMessage
        {
            get { return Messages.Count > 0 && Messages.Exists(p=> p.Code== MessageCode.USERERROR && !string.IsNullOrEmpty(p.Message)); }
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

        public string UserError
        {
            get
            {
                var msg = Messages.Find(p => p.Code == MessageCode.USERERROR);
                if (msg != null)
                    return msg.Message;

                return string.Empty;
            }
        }

        public string SystemError
        {
            get
            {
                var msg = Messages.Find(p => p.Code == MessageCode.SYSTEMERROR);
                if (msg != null)
                    return msg.Message;

                return string.Empty;
            }
        }

        public string Result
        {
            get
            {
                var msg = Messages.Find(p => p.Code == MessageCode.RESULT);
                if (msg != null)
                    return msg.Message;

                return string.Empty;
            }
        }

    }

    public class IntwentyJSONStringResult : IntwentyResult
    {
        public string Data { get; set; }

        public object JElement { get; set; }

        public ApplicationData GetAsApplicationData()
        {
            if (string.IsNullOrEmpty(Data))
                return new ApplicationData();

            if (Data.Length < 5)
                return new ApplicationData();

            var model = System.Text.Json.JsonDocument.Parse(Data).RootElement;
            var result = ApplicationData.CreateFromJSON(model);


            return result;

        }

        public ClientOperation CreateClientState()
        {
            var model = System.Text.Json.JsonDocument.Parse(Data).RootElement;
            var result = ClientOperation.CreateFromJSON(model);
            return result;
        }

        public void AddApplicationJSONValue(string jsonname, object jsonvalue, bool isnumeric = false)
        {
            var check = Data.IndexOf("{", 2);
            if (check < 2)
                return;

            check = Data.IndexOf("}", check);
            if (check < 2)
                return;


            string value = string.Empty;
            if (!isnumeric)
                value = ",\"" + jsonname + "\":" + "\"" + System.Text.Json.JsonEncodedText.Encode(Convert.ToString(jsonvalue)).ToString() + "\"";
            else
                value = ",\"" + jsonname + "\":" + System.Text.Json.JsonEncodedText.Encode(Convert.ToString(jsonvalue)).ToString();

            Data = Data.Insert(check, value);

        }

        public void AddApplicationJSONArray(string jsonname, string jsonaray)
        {
          
            var check = Data.LastIndexOf("}");
            if (check < 1)
                return;

            var value = ",\"" + jsonname + "\":" + jsonaray;
        
            Data = Data.Insert(check, value);

        }

        public void RemoveJSON(string jsonname)
        {
            var cnt = 0;

            if (string.IsNullOrEmpty(jsonname))
                return;

            if (jsonname.Length < 2)
                return;

            while (Data.IndexOf(jsonname) > -1 && cnt < 1000)
            {
                cnt += 1;

                var nameindex = Data.IndexOf(jsonname);
                if (nameindex < 0)
                    continue;

                var test = Data.IndexOf(":", nameindex + jsonname.Length);
                if ((nameindex + jsonname.Length + 3) < test)
                    continue;

                var startindex = Data.LastIndexOf(",", nameindex);
                test = Data.LastIndexOf("{", nameindex);
                if (test > startindex)
                    startindex = test;

                var endindex = Data.IndexOf(",", startindex + 1);
                test = Data.IndexOf("}", startindex + 1);
                if (endindex == -1)
                {
                    endindex = test;
                }
                else
                {
                    if (test < endindex && test > -1)
                        endindex = test;
                }

                var count = (endindex - startindex);
                if (count < 3)
                    continue;

                Data = Data.Remove(startindex, count);

            }
        }
    }

}

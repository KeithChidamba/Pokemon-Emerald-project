using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class TestingUtilities
{
   public string htmlHeader = @"<!DOCTYPE html>
      <html lang=""en"">
      <head>
      <meta charset=""UTF-8"">
      <title>Test Log</title>

      <style>
      body{
          background:#1e1e1e;
          color:#d4d4d4;
          font-family:Consolas, monospace;
          margin:20px;
      }

      h1{
          color:white;
      }

      table{
          width:100%;
          border-collapse:collapse;
      }

      th{
          text-align:left;
          padding:8px;
          border-bottom:2px solid #666;
          color:white;
      }

      td{
          padding:4px 8px;
          border-bottom:1px solid #333;
          vertical-align:top;
      }

      .time{
          color:#888;
          width:220px;
      }

      .type{
          width:120px;
          font-weight:bold;
      }

    .dialogue{
    color:#dcdcaa;      /* Soft yellow - character dialogue */
    }

    .health{
        color:#800080;      /* Red - HP/healing/damage */
    }

    .calculation{
        color:#c586c0;      /* Purple - formulas and calculations */
    }

    .information{
        color:#58a6ff;      /* Blue - general information */
    }

    .error{
        color:#f85149;      /* Bright red - errors */
        font-weight:bold;
    }
    .testcase{
        color:#FFC0CB;      /* Pink */
    }
    .test{
        color:#79c0ff;      /* Cyan - test start/status */
        font-weight:bold;
    }

    .pass{
        color:#3fb950;      /* Green - successful test */
        font-weight:bold;
    }

      .separator td{
          border-bottom:2px solid #666;
      }
      </style>

      </head>

      <body>

      <h1>Test Logs</h1>

      <table>

      <tr>
          <th>Timestamp</th>
          <th>Type</th>
          <th>Message</th>
      </tr>";
   public string htmlFooter = @"
      </table>

      </body>
      </html>";
}

public struct MessageLog
{
    public DateTime timestamp;
    public TestLogType type;
    public string message;
    public MessageLog(DateTime timestamp, string message,TestLogType type)
    {
        this.timestamp = timestamp;
        this.message = message;
        this.type = type;
    }
}
public enum TestLogType
{
    Dialogue,
    Health,
    Calculation,
    Information,
    Error,
    Test,
    Pass,
    TestCase
}

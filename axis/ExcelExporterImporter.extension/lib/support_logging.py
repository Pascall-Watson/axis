# -*- coding: utf-8 -*-
"""Support-oriented structured logging for the pyRevit workflow."""

import datetime
import os
import traceback
import uuid


def _log_root():
    base_path = os.environ.get("LOCALAPPDATA") or os.path.expanduser("~")
    return os.path.join(base_path, "Axis", "ExcelExporterImporter", "Logs")


def ensure_log_directory():
    log_root = _log_root()
    if not os.path.isdir(log_root):
        os.makedirs(log_root)
    return log_root


def get_python_log_path():
    return os.path.join(ensure_log_directory(), "python-ui.log")


def _utc_timestamp():
    return datetime.datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%S.%fZ")


def _escape(value):
    return str(value).replace("\\", "\\\\").replace('"', '\\"').replace("\r", " ").replace("\n", " ")


def write_log(level, event_name, fields=None):
    log_path = get_python_log_path()
    payload = {"timestamp": _utc_timestamp(), "level": level, "event": event_name}
    if fields:
        payload.update(fields)

    ordered_keys = ["timestamp", "level", "event"]
    ordered_keys.extend(sorted([key for key in payload.keys() if key not in ordered_keys]))
    line = " ".join(['{0}="{1}"'.format(key, _escape(payload[key])) for key in ordered_keys if payload.get(key) is not None])

    with open(log_path, "a") as stream:
        stream.write(line + "\n")

    return log_path


def log_exception(event_name, exception, fields=None):
    payload = dict(fields or {})
    payload["exceptionType"] = exception.__class__.__name__
    payload["exceptionMessage"] = str(exception)
    payload["traceback"] = traceback.format_exc()
    return write_log("ERROR", event_name, payload)


class OperationLogger(object):
    def __init__(self, command_name):
        self.command_name = command_name
        self.session_id = uuid.uuid4().hex
        self.backend_log_path = None
        write_log(
            "INFO",
            "ui-command-start",
            {"command": self.command_name, "sessionId": self.session_id},
        )

    def set_backend_log_path(self, backend_log_path):
        if backend_log_path:
            self.backend_log_path = backend_log_path
            write_log(
                "INFO",
                "backend-log-discovered",
                {
                    "command": self.command_name,
                    "sessionId": self.session_id,
                    "backendLogPath": backend_log_path,
                },
            )

    def info(self, event_name, **fields):
        fields["command"] = self.command_name
        fields["sessionId"] = self.session_id
        return write_log("INFO", event_name, fields)

    def warning(self, event_name, **fields):
        fields["command"] = self.command_name
        fields["sessionId"] = self.session_id
        return write_log("WARN", event_name, fields)

    def exception(self, event_name, exception, **fields):
        fields["command"] = self.command_name
        fields["sessionId"] = self.session_id
        return log_exception(event_name, exception, fields)

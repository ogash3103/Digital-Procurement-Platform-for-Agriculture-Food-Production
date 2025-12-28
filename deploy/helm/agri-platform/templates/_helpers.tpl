{{- define "agri-platform.name" -}}
agri-platform
{{- end -}}

{{- define "agri-platform.labels" -}}
app.kubernetes.io/name: {{ include "agri-platform.name" . }}
app.kubernetes.io/managed-by: Helm
{{- end -}}

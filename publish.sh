if command -v podman &> /dev/null; then
  alias docker=podman
else
  echo "podman not installed"
fi

docker build . -t sc:1.0
pause
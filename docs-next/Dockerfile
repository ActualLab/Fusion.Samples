FROM quay.io/jupyter/minimal-notebook:2024-10-29

# Install .NET 8
USER root
RUN source /etc/os-release && \
    wget https://packages.microsoft.com/config/$ID/$VERSION_ID/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    sudo dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    sudo apt update
RUN apt install -y dotnet-sdk-8.0
USER 1000

# Install .NET kernel
RUN dotnet tool install -g --add-source "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" Microsoft.dotnet-interactive
ENV PATH="/${HOME}/.dotnet/tools:${PATH}"
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
RUN dotnet interactive jupyter install

# Setup Tutorial folder
WORKDIR ~
RUN rm -rf work && \
    mkdir tutorial

# Run Jupyter Notebook
ENTRYPOINT ["jupyter", "notebook", "--notebook-dir", "~/tutorial", "--ip=0.0.0.0", "--no-browser", "--NotebookApp.token=''", "--NotebookApp.password=''"]

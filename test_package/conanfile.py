from conans import ConanFile, CMake, tools, RunEnvironment
import os


class TestPackageConan(ConanFile):
    settings = "os", "compiler", "build_type", "arch"
    generators = "cmake"

    def build(self):
        cmake = CMake(self)
        cmake.configure()
        cmake.build()

    def imports(self):
        self.copy("*.dll", dst="bin", src="bin")
        self.copy("*.dylib*", dst="bin", src="lib")
        self.copy('*.so*', dst='bin', src='lib')

    def test(self):
        exe = os.path.join("bin", "test_package")
        with tools.environment_append(RunEnvironment(self).vars):
            if self.settings.os == "Windows":
                self.run(exe)
            else:
                self.run("DYLD_LIBRARY_PATH=%s %s" % (os.environ['DYLD_LIBRARY_PATH'], exe))

const gulp = require("gulp");
const jest = require("gulp-jest").default;
const exec = require("gulp-exec");

async function compile() {
  const options = {
    continueOnError: false, // default = false, true means don't emit error event
    pipeStdout: false, // default = false, true means stdout is written to file.contents
  };
  const reportOptions = {
    err: true, // default = true, false means don't write err
    stderr: true, // default = true, false means don't write stderr
    stdout: true, // default = true, false means don't write stdout
  };

  return gulp
      .src("../Schemas/*.bop")
      .pipe(exec((file) => `..\\..\\bin\\compiler\\Windows-Debug\\bebopc.exe --ts test\\generated\\${file.stem}.ts --files ${file.path}`, options))
      .pipe(exec.reporter(reportOptions));
}

// this doesn't work, because gulp-jest is broken or something
async function test() {
    return gulp.src("test/*.ts").pipe(jest({
        "preprocessorIgnorePatterns": [
            "<rootDir>/dist/", "<rootDir>/node_modules/"
        ],
        "automock": false
    }));
}

exports.compile = compile;
exports.test = gulp.series(compile, test);

# For x64 linux machine
MRuby::CrossBuild.new("linux-x64") do |conf|
  conf.toolchain
  conf.gembox '../../../vitalrouter'

  conf.cc.defines << 'MRB_NO_STDIO'

  conf.compilers.each do |cc|
    cc.flags << '-fPIC'
  end

  conf.archiver do |archiver|
    archiver.command = cc.command
    archiver.archive_options = '-shared -o %{outfile} %{objs}'
  end

  conf.linker do |linker|
    linker.command = ENV['LD'] || 'gcc'
    linker.flags = [ENV['LDFLAGS'] || []]
    linker.libraries = %w(m)
  end
  

  # file extensions
  conf.exts do |exts|
    exts.library = '.so'
  end
end

MRuby::CrossBuild.new("linux-arm64") do |conf|
  conf.toolchain
  conf.gembox '../../../vitalrouter'

  conf.cc.command = 'aarch64-linux-gnu-gcc'
  conf.linker.command = 'aarch64-linux-gnu-gcc'
  conf.archiver.command = 'aarch64-linux-gnu-ar'

  conf.cc.defines << 'MRB_NO_STDIO'

  conf.compilers.each do |cc|
    cc.flags << '-fPIC'
  end

  conf.archiver do |archiver|
    archiver.command = cc.command
    archiver.archive_options = '-shared -o %{outfile} %{objs}'
  end

  conf.linker do |linker|
    linker.command = ENV['LD'] || 'gcc'
    linker.flags = [ENV['LDFLAGS'] || []]
    linker.libraries = %w(m)
  end
  

  # file extensions
  conf.exts do |exts|
    exts.library = '.so'
  end
end

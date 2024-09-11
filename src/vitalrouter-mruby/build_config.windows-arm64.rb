MRuby::CrossBuild.new("windows") do |conf|
  conf.toolchain
  conf.gembox '../../../vitalrouter'
end
